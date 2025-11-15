// Program.cs
// Simple code statistics tool for RC Drag Manager
//
// Usage:
//   1. Set this project as Startup Project
//   2. Run (F5) or Ctrl+F5
//   3. Output: file list + line counts to console
//
// You can also pass a root folder as an argument if needed.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RCDragManager.CodeStats
{
    internal static class Program
    {
        private static readonly HashSet<string> IgnoredDirectories = new HashSet<string>(
            new[]
            {
                ".git",
                ".vs",
                "bin",
                "obj",
                "packages"
            },
            StringComparer.OrdinalIgnoreCase);

        private static void Main(string[] args)
        {
            try
            {
                string rootDirectory = GetRootDirectory(args);

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine(" RC Drag Manager - Code File Line Counts");
                Console.WriteLine(" Root: " + rootDirectory);
                Console.WriteLine(" Time: " + DateTime.Now);
                Console.WriteLine("--------------------------------------------------");

                List<FileStat> stats = GetFileStats(rootDirectory, "*.cs");

                if (stats.Count == 0)
                {
                    Console.WriteLine("No .cs files found. Check the root directory.");
                    PauseIfDebug();
                    return;
                }

                // Sort largest to smallest by total lines
                List<FileStat> sorted = stats
                    .OrderByDescending(s => s.TotalLines)
                    .ThenBy(s => s.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int totalFiles = sorted.Count;
                int totalLines = sorted.Sum(s => s.TotalLines);
                int totalCodeLines = sorted.Sum(s => s.NonEmptyLines);

                Console.WriteLine();
                Console.WriteLine("File".PadRight(60) + "Total".PadLeft(8) + "Code".PadLeft(8));
                Console.WriteLine(new string('-', 76));

                foreach (FileStat stat in sorted)
                {
                    string fileName = stat.RelativePath;
                    if (fileName.Length > 58)
                    {
                        fileName = "..." + fileName.Substring(fileName.Length - 55);
                    }

                    Console.WriteLine(
                        fileName.PadRight(60) +
                        stat.TotalLines.ToString().PadLeft(8) +
                        stat.NonEmptyLines.ToString().PadLeft(8));
                }

                Console.WriteLine(new string('-', 76));
                Console.WriteLine(
                    "TOTAL".PadRight(60) +
                    totalLines.ToString().PadLeft(8) +
                    totalCodeLines.ToString().PadLeft(8));
                Console.WriteLine();
                Console.WriteLine("Files counted: " + totalFiles);

                PauseIfDebug();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL ERROR: " + ex.Message);
                Console.WriteLine(ex);
                PauseIfDebug();
            }
        }

        private static string GetRootDirectory(string[] args)
        {
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                string argPath = args[0].Trim('"', ' ');

                if (Directory.Exists(argPath))
                {
                    Console.WriteLine("[LOG] Using root from args: " + argPath);
                    return Path.GetFullPath(argPath);
                }

                Console.WriteLine("[LOG] Arg path does not exist, falling back to current directory.");
            }

            // Default: solution directory (current directory when running from VS)
            string current = Directory.GetCurrentDirectory();
            Console.WriteLine("[LOG] Using current directory as root: " + current);
            return current;
        }

        private static List<FileStat> GetFileStats(string rootDirectory, string searchPattern)
        {
            List<FileStat> result = new List<FileStat>();

            Console.WriteLine("[LOG] Scanning for " + searchPattern + " under: " + rootDirectory);

            IEnumerable<string> files = EnumerateFilesSafe(rootDirectory, searchPattern);

            foreach (string file in files)
            {
                try
                {
                    string[] lines = File.ReadAllLines(file);
                    int totalLines = lines.Length;
                    int nonEmptyLines = lines.Count(l => !string.IsNullOrWhiteSpace(l));

                    string relative = GetRelativePath(rootDirectory, file);

                    result.Add(new FileStat
                    {
                        FullPath = file,
                        RelativePath = relative,
                        TotalLines = totalLines,
                        NonEmptyLines = nonEmptyLines
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARN] Failed to read file: " + file);
                    Console.WriteLine("       " + ex.Message);
                }
            }

            Console.WriteLine("[LOG] Scan complete. Files found: " + result.Count);
            return result;
        }

        private static IEnumerable<string> EnumerateFilesSafe(string rootDirectory, string searchPattern)
        {
            Stack<string> pending = new Stack<string>();
            pending.Push(rootDirectory);

            while (pending.Count > 0)
            {
                string current = pending.Pop();

                string dirName = Path.GetFileName(current);
                if (!string.IsNullOrEmpty(dirName) && IgnoredDirectories.Contains(dirName))
                {
                    Console.WriteLine("[LOG] Skipping directory: " + current);
                    continue;
                }

                string[] files = Array.Empty<string>();
                string[] subDirs = Array.Empty<string>();

                try
                {
                    files = Directory.GetFiles(current, searchPattern, SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARN] Failed to list files in: " + current);
                    Console.WriteLine("       " + ex.Message);
                }

                foreach (string file in files)
                {
                    yield return file;
                }

                try
                {
                    subDirs = Directory.GetDirectories(current);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARN] Failed to list subdirectories in: " + current);
                    Console.WriteLine("       " + ex.Message);
                }

                foreach (string sub in subDirs)
                {
                    pending.Push(sub);
                }
            }
        }

        private static string GetRelativePath(string rootDirectory, string fullPath)
        {
            string root = rootDirectory;
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                root = root + Path.DirectorySeparatorChar;
            }

            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(root.Length);
            }

            return fullPath;
        }

        private static void PauseIfDebug()
        {
            // Small helper so the window does not close instantly when run from VS
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private sealed class FileStat
        {
            public string FullPath { get; set; }

            public string RelativePath { get; set; }

            public int TotalLines { get; set; }

            public int NonEmptyLines { get; set; }
        }
    }
}
