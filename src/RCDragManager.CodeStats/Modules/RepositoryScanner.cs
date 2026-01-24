using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class RepositoryScanner
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
                @"(?<name>[A-Za-z0-9_]+)" +
                @"(\s*:\s*(?<base>[A-Za-z0-9_.<>,\s]+))?",
                RegexOptions.Compiled);

        private static readonly Regex MethodRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|partial|\s)+\s+" +
                @"([A-Za-z0-9_<>,\[\]\?]+)\s+" +
                @"(?<name>[A-Za-z0-9_]+)\s*\(",
                RegexOptions.Compiled);

        // Find SQL-ish strings
        private static readonly Regex SqlStringRegex =
            new Regex(
                "\"([^\"]*(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE)[^\"]*)\"",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Tables after FROM / JOIN / INTO
        private static readonly Regex TableRegex =
            new Regex(@"\b(FROM|JOIN|INTO)\s+([A-Za-z0-9_\.\[\]]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // SELECT column list (before FROM)
        private static readonly Regex SelectColumnsRegex =
            new Regex(@"SELECT\s+(?<cols>.+?)\s+FROM",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Parameters like @Id
        private static readonly Regex ParameterRegex =
            new Regex(@"@[A-Za-z0-9_]+",
                RegexOptions.Compiled);

        public static List<RepositoryInfo> Scan(string root)
        {
            Console.WriteLine("[SCAN] Repository Scanner");

            List<RepositoryInfo> repositories = new List<RepositoryInfo>();
            int fileCount = 0;

            foreach (string file in EnumerateCsFiles(root))
            {
                fileCount++;
                ScanFile(root, file, repositories);
            }

            Console.WriteLine("[SCAN]   Files scanned       : " + fileCount);
            Console.WriteLine("[SCAN]   Repository classes  : " + repositories.Count);

            WriteJson(root, repositories);
            WriteMarkdown(root, repositories);
            WriteSqlMap(root, repositories);

            return repositories;
        }

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

        private static void ScanFile(string root, string filePath, List<RepositoryInfo> repositories)
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
            string? currentClassName = null;
            string? currentBaseType = null;
            RepositoryInfo? currentRepo = null;
            string? currentMethodName = null;

            bool namespaceLooksRepo = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                Match nsMatch = NamespaceRegex.Match(line);
                if (nsMatch.Success)
                {
                    currentNamespace = nsMatch.Groups[1].Value.Trim();
                    namespaceLooksRepo = currentNamespace.IndexOf(".Repositories", StringComparison.OrdinalIgnoreCase) >= 0;
                    continue;
                }

                Match classMatch = ClassRegex.Match(line);
                if (classMatch.Success)
                {
                    currentClassName = classMatch.Groups["name"].Value.Trim();
                    currentBaseType = classMatch.Groups["base"].Success
                        ? classMatch.Groups["base"].Value.Trim()
                        : null;

                    bool nameLooksRepo = currentClassName.EndsWith("Repository", StringComparison.Ordinal);

                    if (namespaceLooksRepo || nameLooksRepo)
                    {
                        currentRepo = new RepositoryInfo();
                        currentRepo.Name = currentClassName;
                        currentRepo.Namespace = currentNamespace;
                        currentRepo.BaseType = currentBaseType;

                        if (!string.IsNullOrWhiteSpace(currentNamespace))
                        {
                            currentRepo.FullName = currentNamespace + "." + currentClassName;
                        }
                        else
                        {
                            currentRepo.FullName = currentClassName;
                        }

                        currentRepo.FilePath = Path.GetRelativePath(root, filePath);
                        repositories.Add(currentRepo);
                    }
                    else
                    {
                        currentRepo = null;
                    }

                    currentMethodName = null;
                    continue;
                }

                Match methodMatch = MethodRegex.Match(line);
                if (methodMatch.Success)
                {
                    currentMethodName = methodMatch.Groups["name"].Value.Trim();
                }

                if (currentRepo == null)
                {
                    continue;
                }

                Match sqlMatch = SqlStringRegex.Match(line);
                if (!sqlMatch.Success)
                {
                    continue;
                }

                string snippet = sqlMatch.Groups[1].Value.Trim();
                snippet = CollapseWhitespace(snippet);

                RepositorySqlUsage usage = new RepositorySqlUsage();
                usage.MethodName = currentMethodName;
                usage.LineNumber = i + 1;
                usage.Snippet = snippet;

                AnalyzeSql(snippet, usage);

                currentRepo.SqlUsages.Add(usage);
            }
        }

        private static void AnalyzeSql(string snippet, RepositorySqlUsage usage)
        {
            if (string.IsNullOrWhiteSpace(snippet))
            {
                return;
            }

            string upper = snippet.ToUpperInvariant();

            if (upper.Contains("SELECT"))
            {
                usage.CommandType = "SELECT";
            }
            else if (upper.Contains("INSERT"))
            {
                usage.CommandType = "INSERT";
            }
            else if (upper.Contains("UPDATE"))
            {
                usage.CommandType = "UPDATE";
            }
            else if (upper.Contains("DELETE"))
            {
                usage.CommandType = "DELETE";
            }

            MatchCollection tableMatches = TableRegex.Matches(snippet);

            for (int i = 0; i < tableMatches.Count; i++)
            {
                string table = tableMatches[i].Groups[2].Value.Trim();

                if (!usage.Tables.Contains(table))
                {
                    usage.Tables.Add(table);
                }
            }

            Match selectMatch = SelectColumnsRegex.Match(snippet);
            if (selectMatch.Success)
            {
                string cols = selectMatch.Groups["cols"].Value;
                string[] parts = cols.Split(',');

                for (int i = 0; i < parts.Length; i++)
                {
                    string col = parts[i].Trim();

                    if (col.StartsWith("DISTINCT ", StringComparison.OrdinalIgnoreCase))
                    {
                        col = col.Substring("DISTINCT ".Length).Trim();
                    }

                    if (col.Length == 0)
                    {
                        continue;
                    }

                    if (!usage.Columns.Contains(col))
                    {
                        usage.Columns.Add(col);
                    }
                }
            }

            MatchCollection paramMatches = ParameterRegex.Matches(snippet);

            for (int i = 0; i < paramMatches.Count; i++)
            {
                string param = paramMatches[i].Value;

                if (!usage.Parameters.Contains(param))
                {
                    usage.Parameters.Add(param);
                }
            }
        }

        private static string CollapseWhitespace(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(input.Length);
            bool previousWasSpace = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (char.IsWhiteSpace(c))
                {
                    if (!previousWasSpace)
                    {
                        sb.Append(' ');
                        previousWasSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    previousWasSpace = false;
                }
            }

            return sb.ToString().Trim();
        }

        private static void WriteJson(string root, List<RepositoryInfo> repositories)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Repositories.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(repositories, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<RepositoryInfo> repositories)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Repositories.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Repositories and SQL Usage");
            sb.AppendLine();
            sb.AppendLine("Total repositories: " + repositories.Count);
            sb.AppendLine();

            for (int i = 0; i < repositories.Count; i++)
            {
                RepositoryInfo repo = repositories[i];

                string heading = repo.FullName ?? repo.Name;
                sb.AppendLine("## " + heading);
                sb.AppendLine();

                sb.AppendLine("- File: `" + repo.FilePath + "`");

                if (!string.IsNullOrWhiteSpace(repo.BaseType))
                {
                    sb.AppendLine("- Base: `" + repo.BaseType + "`");
                }

                sb.AppendLine("- SQL usages: " + repo.SqlUsages.Count);
                sb.AppendLine();

                for (int j = 0; j < repo.SqlUsages.Count; j++)
                {
                    RepositorySqlUsage usage = repo.SqlUsages[j];

                    sb.Append("- [");
                    sb.Append(repo.FilePath);
                    sb.Append(":");
                    sb.Append(usage.LineNumber);
                    sb.Append("] ");

                    if (!string.IsNullOrWhiteSpace(usage.MethodName))
                    {
                        sb.Append(usage.MethodName);
                        sb.Append(" : ");
                    }

                    if (!string.IsNullOrWhiteSpace(usage.CommandType))
                    {
                        sb.Append(usage.CommandType);
                        sb.Append(" ");
                    }

                    sb.Append("`");
                    sb.Append(usage.Snippet);
                    sb.AppendLine("`");

                    if (usage.Tables.Count > 0)
                    {
                        sb.Append("  - Tables: ");
                        sb.AppendLine(string.Join(", ", usage.Tables));
                    }

                    if (usage.Columns.Count > 0)
                    {
                        sb.Append("  - Columns: ");
                        sb.AppendLine(string.Join(", ", usage.Columns));
                    }

                    if (usage.Parameters.Count > 0)
                    {
                        sb.Append("  - Params: ");
                        sb.AppendLine(string.Join(", ", usage.Parameters));
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteSqlMap(string root, List<RepositoryInfo> repositories)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "SqlMap.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(repositories, options);
            File.WriteAllText(path, json);
        }
    }
}
