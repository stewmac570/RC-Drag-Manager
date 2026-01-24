using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class MethodScanner
    {
        // Same skip rules as ClassScanner
        private static readonly HashSet<string> SkippedDirectoryNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                ".vs",
                "packages",
                ".git",
                "RCDragManager.CodeStats"
            };

        // namespace Foo.Bar
        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        // class MyClass
        private static readonly Regex ClassRegex =
            new Regex(@"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+([A-Za-z0-9_]+)",
                      RegexOptions.Compiled);

        // Rough method signature:
        // public async Task Foo(int x, string y) {
        // private int Bar() =>
        private static readonly Regex MethodRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|partial|\s)+\s+" +
                @"([A-Za-z0-9_<>,\[\]\?]+)\s+" +    // return type
                @"([A-Za-z0-9_]+)\s*\(([^)]*)\)\s*" + // name + (params)
                @"(\{|=>|where|\;)?\s*$",
                RegexOptions.Compiled);

        // Keywords we never want to treat as method names
        private static readonly HashSet<string> ControlKeywords =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "if",
                "for",
                "foreach",
                "while",
                "switch",
                "catch",
                "using",
                "lock"
            };

        public static List<MethodInfo> Scan(string root)
        {
            Console.WriteLine("[SCAN] Method Scanner");

            List<MethodInfo> methods = new List<MethodInfo>();
            int fileCount = 0;

            foreach (string file in EnumerateCsFiles(root))
            {
                fileCount++;
                ScanFile(root, file, methods);
            }

            Console.WriteLine($"[SCAN]   Files scanned : {fileCount}");
            Console.WriteLine($"[SCAN]   Methods found : {methods.Count}");

            WriteJson(root, methods);
            WriteMarkdown(root, methods);

            return methods;
        }

        // ─────────────────────────────────────────────────────────────
        // File enumeration (shared pattern with ClassScanner)
        // ─────────────────────────────────────────────────────────────
        private static IEnumerable<string> EnumerateCsFiles(string root)
        {
            Stack<string> pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string dir = pending.Pop();
                string dirName = Path.GetFileName(dir);

                if (SkippedDirectoryNames.Contains(dirName))
                {
                    continue;
                }

                string[] subDirs;

                try
                {
                    subDirs = Directory.GetDirectories(dir);
                }
                catch (Exception)
                {
                    continue;
                }

                for (int i = 0; i < subDirs.Length; i++)
                {
                    pending.Push(subDirs[i]);
                }

                string[] files;

                try
                {
                    files = Directory.GetFiles(dir, "*.cs");
                }
                catch (Exception)
                {
                    continue;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    yield return files[i];
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Per-file parsing
        // ─────────────────────────────────────────────────────────────
        private static void ScanFile(string root, string filePath, List<MethodInfo> methods)
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (Exception)
            {
                return;
            }

            string? currentNamespace = null;
            string? currentClass = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // namespace
                Match nsMatch = NamespaceRegex.Match(line);
                if (nsMatch.Success)
                {
                    currentNamespace = nsMatch.Groups[1].Value.Trim();
                    continue;
                }

                // class
                Match classMatch = ClassRegex.Match(line);
                if (classMatch.Success)
                {
                    currentClass = classMatch.Groups[2].Value.Trim();
                    continue;
                }

                // quick filters: must contain "(" and ")"
                if (!line.Contains("(") || !line.Contains(")"))
                {
                    continue;
                }

                // ignore lambda lines like "=> (x) => ..."
                if (line.Contains("=>"))
                {
                    // but still let the regex decide — lambdas usually don't match our pattern
                }

                Match methodMatch = MethodRegex.Match(line);
                if (!methodMatch.Success)
                {
                    continue;
                }

                string methodName = methodMatch.Groups[3].Value.Trim();

                // skip control-flow "methods" like if/for/while
                if (ControlKeywords.Contains(methodName))
                {
                    continue;
                }

                string returnType = methodMatch.Groups[2].Value.Trim();
                string parameters = methodMatch.Groups[4].Value.Trim();
                bool isAsync = line.Contains("async ");

                MethodInfo info = new MethodInfo();
                info.Name = methodName;
                info.ReturnType = returnType;
                info.Namespace = currentNamespace;
                info.DeclaringType = currentClass;
                info.DeclaringFullName = BuildFullTypeName(currentNamespace, currentClass);
                info.FilePath = Path.GetRelativePath(root, filePath);
                info.LineNumber = i + 1;
                info.ParameterSignature = parameters;
                info.IsAsync = isAsync;

                methods.Add(info);
            }
        }

        private static string? BuildFullTypeName(string? ns, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(ns))
            {
                return typeName;
            }

            return ns + "." + typeName;
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string root, List<MethodInfo> methods)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Methods.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(methods, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<MethodInfo> methods)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Methods.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Method List");
            sb.AppendLine();
            sb.AppendLine($"Total methods: {methods.Count}");
            sb.AppendLine();

            foreach (MethodInfo m in methods)
            {
                string typePrefix = string.IsNullOrWhiteSpace(m.DeclaringFullName)
                    ? string.Empty
                    : m.DeclaringFullName + ".";

                sb.Append("- ");
                sb.Append(typePrefix);
                sb.Append(m.Name);
                sb.Append("(");
                sb.Append(m.ParameterSignature);
                sb.Append(")");

                if (!string.IsNullOrWhiteSpace(m.ReturnType))
                {
                    sb.Append(" : ");
                    sb.Append(m.ReturnType);
                }

                sb.Append("  [");
                sb.Append(m.FilePath);
                sb.Append(":");
                sb.Append(m.LineNumber);
                sb.AppendLine("]");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
