using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class ClassScanner
    {
        // Directories we never want to walk
        private static readonly HashSet<string> SkippedDirectoryNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                ".vs",
                "packages",
                ".git",
                "RCDragManager.CodeStats" // do not analyze ourselves
            };

        // Rough regexes – good enough for our codebase
        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        private static readonly Regex ClassRegex =
            new Regex(@"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+([A-Za-z0-9_]+)",
                      RegexOptions.Compiled);

        public static List<ClassInfo> Scan(string root)
        {
            Console.WriteLine("[SCAN] Class Scanner");

            List<ClassInfo> classes = new List<ClassInfo>();
            List<string> files = new List<string>();

            foreach (string file in EnumerateCsFiles(root))
            {
                files.Add(file);
                ScanFile(root, file, classes);
            }

            Console.WriteLine($"[SCAN]   Files scanned : {files.Count}");
            Console.WriteLine($"[SCAN]   Classes found : {classes.Count}");

            WriteJson(root, classes);
            WriteMarkdown(root, classes);

            return classes;
        }

        // ─────────────────────────────────────────────────────────────
        // File enumeration with defensive directory skipping
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

                foreach (string sub in subDirs)
                {
                    pending.Push(sub);
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
        private static void ScanFile(string root, string filePath, List<ClassInfo> classes)
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

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                Match nsMatch = NamespaceRegex.Match(line);
                if (nsMatch.Success)
                {
                    currentNamespace = nsMatch.Groups[1].Value.Trim();
                    continue;
                }

                Match classMatch = ClassRegex.Match(line);
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[2].Value.Trim();

                    ClassInfo info = new ClassInfo();
                    info.Name = className;
                    info.Namespace = currentNamespace;
                    info.FilePath = Path.GetRelativePath(root, filePath);

                    classes.Add(info);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown for this module
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string root, List<ClassInfo> classes)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Classes.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(classes, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<ClassInfo> classes)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Classes.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Class List");
            sb.AppendLine();
            sb.AppendLine($"Total classes: {classes.Count}");
            sb.AppendLine();

            foreach (ClassInfo c in classes)
            {
                sb.Append("- ");
                sb.Append(string.IsNullOrWhiteSpace(c.Namespace) ? c.Name : c.Namespace + "." + c.Name);
                sb.Append("  (");
                sb.Append(c.FilePath);
                sb.AppendLine(")");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
