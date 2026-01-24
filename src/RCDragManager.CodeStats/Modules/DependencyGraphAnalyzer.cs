using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class DependencyGraphAnalyzer
    {
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

        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        private static readonly Regex ClassRegex =
            new Regex(
                @"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+" +
                @"(?<name>[A-Za-z0-9_]+)",
                RegexOptions.Compiled);

        // Field / property declaration:
        // public SomeType _field;
        // private List<Foo> _items = new List<Foo>();
        private static readonly Regex FieldOrPropertyRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|readonly|volatile|\s)+\s+" +
                @"(?<type>[A-Za-z0-9_.<>,\[\]\?]+)\s+" +
                @"(?<name>[A-Za-z0-9_]+)\s*(=|;|\{)",
                RegexOptions.Compiled);

        // "new SomeType(..."
        private static readonly Regex NewExpressionRegex =
            new Regex(
                @"new\s+(?<type>[A-Za-z0-9_\.]+)\s*\(",
                RegexOptions.Compiled);

        // Primitive / framework types to ignore when guessing
        private static readonly HashSet<string> IgnoredTypeNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "void",
                "bool",
                "byte",
                "sbyte",
                "short",
                "ushort",
                "int",
                "uint",
                "long",
                "ulong",
                "float",
                "double",
                "decimal",
                "char",
                "string",
                "object",

                // common generic containers / helpers
                "List",
                "IList",
                "ICollection",
                "IEnumerable",
                "IReadOnlyList",
                "Dictionary",
                "IDictionary",
                "Task",
                "ValueTask",
                "Func",
                "Action",
                "Nullable"
            };

        public static List<DependencyInfo> Analyze(
            string root,
            List<ClassInfo> classes,
            List<MethodInfo> methods)
        {
            Console.WriteLine("[SCAN] Dependency Graph Analyzer");

            List<DependencyInfo> dependencies = new List<DependencyInfo>();

            // Class lookup maps
            Dictionary<string, ClassInfo> classesByFullName =
                new Dictionary<string, ClassInfo>(StringComparer.Ordinal);

            Dictionary<string, List<ClassInfo>> classesByShortName =
                new Dictionary<string, List<ClassInfo>>(StringComparer.Ordinal);

            for (int i = 0; i < classes.Count; i++)
            {
                ClassInfo c = classes[i];

                if (string.IsNullOrWhiteSpace(c.FullName))
                {
                    continue;
                }

                if (!classesByFullName.ContainsKey(c.FullName))
                {
                    classesByFullName[c.FullName] = c;
                }

                string shortName = c.Name;

                if (!classesByShortName.TryGetValue(shortName, out List<ClassInfo> list))
                {
                    list = new List<ClassInfo>();
                    classesByShortName[shortName] = list;
                }

                list.Add(c);
            }

            HashSet<string> edgeKeys = new HashSet<string>(StringComparer.Ordinal);

            // 1) Method return + parameter dependencies
            AddMethodDependencies(methods, classesByShortName, classesByFullName, dependencies, edgeKeys);

            // 2) Field / property + new-expression dependencies from source files
            AddFieldAndNewDependencies(root, classes, classesByShortName, classesByFullName, dependencies, edgeKeys);

            Console.WriteLine("[SCAN]   Dependencies found : " + dependencies.Count);

            WriteJson(root, dependencies);
            WriteMarkdown(root, dependencies);

            return dependencies;
        }

        // ─────────────────────────────────────────────────────────────
        // Method-based dependencies
        // ─────────────────────────────────────────────────────────────
        private static void AddMethodDependencies(
            List<MethodInfo> methods,
            Dictionary<string, List<ClassInfo>> classesByShortName,
            Dictionary<string, ClassInfo> classesByFullName,
            List<DependencyInfo> dependencies,
            HashSet<string> edgeKeys)
        {
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo m = methods[i];

                if (string.IsNullOrWhiteSpace(m.DeclaringFullName))
                {
                    continue;
                }

                if (!classesByFullName.TryGetValue(m.DeclaringFullName, out ClassInfo fromClass))
                {
                    continue;
                }

                // Return type
                if (!string.IsNullOrWhiteSpace(m.ReturnType))
                {
                    AddTypeDependency(
                        "MethodReturn",
                        m.DeclaringFullName,
                        fromClass.FilePath,
                        m.Name,
                        m.LineNumber,
                        m.ReturnType,
                        classesByShortName,
                        classesByFullName,
                        dependencies,
                        edgeKeys);
                }

                // Parameters
                if (!string.IsNullOrWhiteSpace(m.ParameterSignature))
                {
                    string[] paramParts = m.ParameterSignature.Split(',');

                    for (int p = 0; p < paramParts.Length; p++)
                    {
                        string param = paramParts[p].Trim();
                        if (param.Length == 0)
                        {
                            continue;
                        }

                        string paramTypeFull = ExtractTypeName(param);
                        if (string.IsNullOrWhiteSpace(paramTypeFull))
                        {
                            continue;
                        }

                        AddTypeDependency(
                            "MethodParameter",
                            m.DeclaringFullName,
                            fromClass.FilePath,
                            m.Name,
                            m.LineNumber,
                            paramTypeFull,
                            classesByShortName,
                            classesByFullName,
                            dependencies,
                            edgeKeys);
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Field/property + new-expression dependencies from source
        // ─────────────────────────────────────────────────────────────
        private static void AddFieldAndNewDependencies(
            string root,
            List<ClassInfo> classes,
            Dictionary<string, List<ClassInfo>> classesByShortName,
            Dictionary<string, ClassInfo> classesByFullName,
            List<DependencyInfo> dependencies,
            HashSet<string> edgeKeys)
        {
            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < classes.Count; i++)
            {
                string abs = Path.Combine(root, classes[i].FilePath);
                if (!files.Contains(abs))
                {
                    files.Add(abs);
                }
            }

            foreach (string filePath in files)
            {
                string[] lines;

                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch (Exception)
                {
                    continue;
                }

                string? currentNamespace = null;
                string? currentClassName = null;
                string? currentFullName = null;
                ClassInfo? currentClass = null;

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];

                    Match nsMatch = NamespaceRegex.Match(line);
                    if (nsMatch.Success)
                    {
                        currentNamespace = nsMatch.Groups[1].Value.Trim();
                        currentFullName = null;
                        currentClass = null;
                        continue;
                    }

                    Match classMatch = ClassRegex.Match(line);
                    if (classMatch.Success)
                    {
                        currentClassName = classMatch.Groups["name"].Value.Trim();

                        if (!string.IsNullOrWhiteSpace(currentNamespace))
                        {
                            currentFullName = currentNamespace + "." + currentClassName;
                        }
                        else
                        {
                            currentFullName = currentClassName;
                        }

                        if (!string.IsNullOrWhiteSpace(currentFullName))
                        {
                            classesByFullName.TryGetValue(currentFullName, out currentClass);
                        }
                        else
                        {
                            currentClass = null;
                        }

                        continue;
                    }

                    if (currentClass == null || string.IsNullOrWhiteSpace(currentFullName))
                    {
                        continue;
                    }

                    // Field / property type
                    Match memberMatch = FieldOrPropertyRegex.Match(line);
                    if (memberMatch.Success)
                    {
                        string typeName = memberMatch.Groups["type"].Value.Trim();
                        string memberName = memberMatch.Groups["name"].Value.Trim();

                        AddTypeDependency(
                            "FieldOrPropertyType",
                            currentFullName,
                            currentClass.FilePath,
                            memberName,
                            lineIndex + 1,
                            typeName,
                            classesByShortName,
                            classesByFullName,
                            dependencies,
                            edgeKeys);
                    }

                    // new-expression type
                    Match newMatch = NewExpressionRegex.Match(line);
                    if (newMatch.Success)
                    {
                        string newType = newMatch.Groups["type"].Value.Trim();

                        AddTypeDependency(
                            "NewExpression",
                            currentFullName,
                            currentClass.FilePath,
                            "<new>",
                            lineIndex + 1,
                            newType,
                            classesByShortName,
                            classesByFullName,
                            dependencies,
                            edgeKeys);
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Core helper: expand a type text into candidate short names
        // and add edges for any matching classes
        // ─────────────────────────────────────────────────────────────
        private static void AddTypeDependency(
            string kind,
            string fromFullName,
            string fromFilePath,
            string fromMemberName,
            int lineNumber,
            string typeText,
            Dictionary<string, List<ClassInfo>> classesByShortName,
            Dictionary<string, ClassInfo> classesByFullName,
            List<DependencyInfo> dependencies,
            HashSet<string> edgeKeys)
        {
            if (string.IsNullOrWhiteSpace(typeText))
            {
                return;
            }

            List<string> candidates = ExtractCandidateTypeShortNames(typeText);
            if (candidates.Count == 0)
            {
                return;
            }

            for (int c = 0; c < candidates.Count; c++)
            {
                string shortName = candidates[c];

                if (!classesByShortName.TryGetValue(shortName, out List<ClassInfo> targets))
                {
                    continue;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ClassInfo targetClass = targets[i];

                    if (string.Equals(targetClass.FullName, fromFullName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    DependencyInfo info = new DependencyInfo();
                    info.FromFullName = fromFullName;
                    info.ToFullName = targetClass.FullName;
                    info.FromFilePath = fromFilePath;
                    info.ToFilePath = targetClass.FilePath;
                    info.FromMemberName = fromMemberName;
                    info.Kind = kind;
                    info.LineNumber = lineNumber;

                    string key = info.FromFullName + "|" + info.ToFullName + "|" + info.Kind + "|" + info.FromMemberName;
                    if (edgeKeys.Contains(key))
                    {
                        continue;
                    }

                    edgeKeys.Add(key);
                    dependencies.Add(info);
                }
            }
        }

        private static string ExtractTypeName(string parameter)
        {
            string cleaned = parameter
                .Replace("ref ", string.Empty)
                .Replace("out ", string.Empty)
                .Replace("in ", string.Empty)
                .Replace("this ", string.Empty)
                .Trim();

            string[] tokens = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return string.Empty;
            }

            // first token is usually the type
            return tokens[0];
        }

        private static List<string> ExtractCandidateTypeShortNames(string typeText)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrWhiteSpace(typeText))
            {
                return result;
            }

            // Example inputs:
            // System.Collections.Generic.List<RCDragManagerProd.Models.Run>
            // Dictionary<string, RCDragManagerProd.Data.Session>
            // SomeNamespace.SomeType[]
            // Run
            //
            // Strategy:
            // - find identifier sequences (with optional dots)
            // - for each, take last segment after '.'
            // - skip primitives / common generic helpers

            Regex tokenRegex = new Regex(@"[A-Za-z_][A-Za-z0-9_\.]*", RegexOptions.Compiled);
            MatchCollection matches = tokenRegex.Matches(typeText);

            for (int i = 0; i < matches.Count; i++)
            {
                string token = matches[i].Value;

                // last segment after dot
                string shortName = token;
                int dotIdx = token.LastIndexOf('.');
                if (dotIdx >= 0 && dotIdx < token.Length - 1)
                {
                    shortName = token.Substring(dotIdx + 1);
                }

                if (IgnoredTypeNames.Contains(shortName))
                {
                    continue;
                }

                if (!result.Contains(shortName))
                {
                    result.Add(shortName);
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string root, List<DependencyInfo> dependencies)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Dependencies.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(dependencies, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<DependencyInfo> dependencies)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Dependencies.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Dependency Graph");
            sb.AppendLine();
            sb.AppendLine("Total dependencies: " + dependencies.Count);
            sb.AppendLine();

            Dictionary<string, List<DependencyInfo>> byFrom =
                new Dictionary<string, List<DependencyInfo>>(StringComparer.Ordinal);

            for (int i = 0; i < dependencies.Count; i++)
            {
                DependencyInfo d = dependencies[i];

                if (!byFrom.TryGetValue(d.FromFullName, out List<DependencyInfo> list))
                {
                    list = new List<DependencyInfo>();
                    byFrom[d.FromFullName] = list;
                }

                list.Add(d);
            }

            foreach (KeyValuePair<string, List<DependencyInfo>> kvp in byFrom)
            {
                sb.AppendLine("## " + kvp.Key);
                sb.AppendLine();

                List<DependencyInfo> list = kvp.Value;
                list.Sort((a, b) => string.CompareOrdinal(a.ToFullName, b.ToFullName));

                for (int i = 0; i < list.Count; i++)
                {
                    DependencyInfo d = list[i];
                    sb.Append("- ");
                    sb.Append(d.Kind);
                    sb.Append(": ");
                    sb.Append(d.ToFullName);
                    sb.Append("  [");
                    sb.Append(d.FromFilePath);
                    sb.Append(":");
                    sb.Append(d.LineNumber);
                    sb.AppendLine("]");
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
