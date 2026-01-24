using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class ClassRelationAnalyzer
    {
        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        private static readonly Regex ClassWithBaseRegex =
            new Regex(
                @"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+" +
                @"(?<name>[A-Za-z0-9_]+)\s*:\s*(?<bases>[A-Za-z0-9_.<>,\s]+)",
                RegexOptions.Compiled);

        public static List<ClassRelationInfo> Analyze(
            string root,
            List<ClassInfo> classes,
            List<DependencyInfo> dependencies)
        {
            Console.WriteLine("[SCAN] Class Relation Analyzer");

            Dictionary<string, ClassRelationInfo> relations =
                new Dictionary<string, ClassRelationInfo>(StringComparer.Ordinal);

            for (int i = 0; i < classes.Count; i++)
            {
                ClassInfo c = classes[i];

                if (string.IsNullOrWhiteSpace(c.FullName))
                {
                    continue;
                }

                ClassRelationInfo rel = new ClassRelationInfo();
                rel.FullName = c.FullName;
                rel.FilePath = c.FilePath;
                relations[c.FullName] = rel;
            }

            foreach (ClassInfo c in classes)
            {
                string full = c.FullName;
                if (string.IsNullOrWhiteSpace(full))
                {
                    continue;
                }

                string absolute = Path.Combine(root, c.FilePath);
                ParseBaseAndInterfaces(absolute, full, relations);
            }

            for (int i = 0; i < dependencies.Count; i++)
            {
                DependencyInfo d = dependencies[i];

                if (!relations.TryGetValue(d.FromFullName, out ClassRelationInfo rel))
                {
                    continue;
                }

                if (!rel.ComposesTypes.Contains(d.ToFullName))
                {
                    rel.ComposesTypes.Add(d.ToFullName);
                }
            }

            List<ClassRelationInfo> list = new List<ClassRelationInfo>();
            foreach (ClassRelationInfo rel in relations.Values)
            {
                list.Add(rel);
            }

            WriteJson(root, list);
            WriteMarkdown(root, list);

            return list;
        }

        private static void ParseBaseAndInterfaces(
            string absoluteFile,
            string classFullName,
            Dictionary<string, ClassRelationInfo> relations)
        {
            if (!relations.TryGetValue(classFullName, out ClassRelationInfo rel))
            {
                return;
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(absoluteFile);
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

                Match classMatch = ClassWithBaseRegex.Match(line);
                if (!classMatch.Success)
                {
                    continue;
                }

                string name = classMatch.Groups["name"].Value.Trim();

                string full;
                if (!string.IsNullOrWhiteSpace(currentNamespace))
                {
                    full = currentNamespace + "." + name;
                }
                else
                {
                    full = name;
                }

                if (!string.Equals(full, classFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                string bases = classMatch.Groups["bases"].Value;
                string[] parts = bases.Split(',');

                bool baseTypeSet = false;

                for (int j = 0; j < parts.Length; j++)
                {
                    string part = parts[j].Trim();

                    if (part.Length == 0)
                    {
                        continue;
                    }

                    if (!baseTypeSet)
                    {
                        rel.BaseType = part;
                        baseTypeSet = true;
                    }
                    else
                    {
                        if (!rel.Interfaces.Contains(part))
                        {
                            rel.Interfaces.Add(part);
                        }
                    }
                }

                break;
            }
        }

        private static void WriteJson(string root, List<ClassRelationInfo> list)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "ClassRelations.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(list, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<ClassRelationInfo> list)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "ClassRelations.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Class Relationships");
            sb.AppendLine();
            sb.AppendLine("Total classes: " + list.Count);
            sb.AppendLine();

            for (int i = 0; i < list.Count; i++)
            {
                ClassRelationInfo rel = list[i];

                sb.AppendLine("## " + rel.FullName);
                sb.AppendLine();

                sb.AppendLine("- File: `" + rel.FilePath + "`");

                if (!string.IsNullOrWhiteSpace(rel.BaseType))
                {
                    sb.AppendLine("- Base: `" + rel.BaseType + "`");
                }

                if (rel.Interfaces.Count > 0)
                {
                    sb.AppendLine("- Interfaces: " + string.Join(", ", rel.Interfaces));
                }

                if (rel.ComposesTypes.Count > 0)
                {
                    sb.AppendLine("- Composes:");
                    for (int j = 0; j < rel.ComposesTypes.Count; j++)
                    {
                        sb.AppendLine("  - " + rel.ComposesTypes[j]);
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
