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

        // namespace RCDragManagerProd.Repositories
        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        // class RaceSessionRepository : ISomething
        private static readonly Regex ClassRegex =
            new Regex(
                @"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+" +
                @"(?<name>[A-Za-z0-9_]+)" +
                @"(\s*:\s*(?<base>[A-Za-z0-9_.<>,\s]+))?",
                RegexOptions.Compiled);

        // Method signature (same pattern used earlier, simplified)
        private static readonly Regex MethodRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|partial|\s)+\s+" +
                @"([A-Za-z0-9_<>,\[\]\?]+)\s+" +      // return type
                @"(?<name>[A-Za-z0-9_]+)\s*\(",
                RegexOptions.Compiled);

        // Very rough SQL string finder: looks for string literals with SQL keywords
        private static readonly Regex SqlStringRegex =
            new Regex(
                "\"([^\"]*(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE)[^\"]*)\"",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            Console.WriteLine($"[SCAN]   Files scanned       : {fileCount}");
            Console.WriteLine($"[SCAN]   Repository classes  : {repositories.Count}");

            WriteJson(root, repositories);
            WriteMarkdown(root, repositories);

            return repositories;
        }

        // ─────────────────────────────────────────────────────────────
        // File enumeration – all *.cs
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

            // We treat a file as potentially repo-related if:
            // - it has namespace with ".Repositories" OR
            // - it has a class ending in "Repository"
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

                // track methods to attach SQL to methods
                Match methodMatch = MethodRegex.Match(line);
                if (methodMatch.Success)
                {
                    currentMethodName = methodMatch.Groups["name"].Value.Trim();
                }

                if (currentRepo == null)
                {
                    continue;
                }

                // SQL strings inside repository class scope
                Match sqlMatch = SqlStringRegex.Match(line);
                if (!sqlMatch.Success)
                {
                    continue;
                }

                string snippet = sqlMatch.Groups[1].Value.Trim();

                // compress whitespace for brevity
                snippet = CollapseWhitespace(snippet);

                RepositorySqlUsage usage = new RepositorySqlUsage();
                usage.MethodName = currentMethodName;
                usage.LineNumber = i + 1;
                usage.Snippet = snippet;

                currentRepo.SqlUsages.Add(usage);
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

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown
        // ─────────────────────────────────────────────────────────────
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
            sb.AppendLine($"Total repositories: {repositories.Count}");
            sb.AppendLine();

            foreach (RepositoryInfo repo in repositories)
            {
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

                foreach (RepositorySqlUsage usage in repo.SqlUsages)
                {
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

                    sb.Append("`");
                    sb.Append(usage.Snippet);
                    sb.AppendLine("`");
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
