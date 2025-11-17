using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class EventScanner
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

        // namespace Foo.Bar
        private static readonly Regex NamespaceRegex =
            new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        // class MyClass
        private static readonly Regex ClassRegex =
            new Regex(@"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+([A-Za-z0-9_]+)",
                      RegexOptions.Compiled);

        // event declaration:
        // public event EventHandler SomethingHappened;
        // private static event Action FooBar;
        private static readonly Regex EventFieldRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|virtual|override|sealed|\s)*\s*event\s+" +
                @"([A-Za-z0-9_<>,\[\]\.?]+)\s+" +
                @"([A-Za-z0-9_]+)\s*(;|=)",
                RegexOptions.Compiled);

        // method signature similar to MethodScanner
        private static readonly Regex MethodRegex =
            new Regex(
                @"^\s*(public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|partial|\s)+\s+" +
                @"([A-Za-z0-9_<>,\[\]\?]+)\s+" +      // return type
                @"([A-Za-z0-9_]+)\s*\(([^)]*)\)\s*" + // name + (params)
                @"(\{|=>|where|\;)?\s*$",
                RegexOptions.Compiled);

        // names we don't treat as "handlers"
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

        public static List<EventInfo> Scan(string root)
        {
            Console.WriteLine("[SCAN] Event Scanner");

            List<EventInfo> events = new List<EventInfo>();
            int fileCount = 0;

            foreach (string file in EnumerateCsFiles(root))
            {
                fileCount++;
                ScanFile(root, file, events);
            }

            Console.WriteLine($"[SCAN]   Files scanned : {fileCount}");
            Console.WriteLine($"[SCAN]   Events found  : {events.Count}");

            WriteJson(root, events);
            WriteMarkdown(root, events);

            return events;
        }

        // ─────────────────────────────────────────────────────────────
        // File enumeration
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
        private static void ScanFile(string root, string filePath, List<EventInfo> events)
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

                // namespace Foo.Bar
                Match nsMatch = NamespaceRegex.Match(line);
                if (nsMatch.Success)
                {
                    currentNamespace = nsMatch.Groups[1].Value.Trim();
                    continue;
                }

                // class MyClass
                Match classMatch = ClassRegex.Match(line);
                if (classMatch.Success)
                {
                    currentClass = classMatch.Groups[2].Value.Trim();
                    continue;
                }

                // event fields
                Match eventFieldMatch = EventFieldRegex.Match(line);
                if (eventFieldMatch.Success)
                {
                    string eventType = eventFieldMatch.Groups[2].Value.Trim();
                    string eventName = eventFieldMatch.Groups[3].Value.Trim();

                    EventInfo info = new EventInfo();
                    info.Kind = "EventField";
                    info.Name = eventName;
                    info.EventType = eventType;
                    info.Namespace = currentNamespace;
                    info.DeclaringType = currentClass;
                    info.DeclaringFullName = BuildFullTypeName(currentNamespace, currentClass);
                    info.FilePath = Path.GetRelativePath(root, filePath);
                    info.LineNumber = i + 1;

                    events.Add(info);
                    continue;
                }

                // potential handler methods (must have "(" and ")")
                if (!line.Contains("(") || !line.Contains(")"))
                {
                    continue;
                }

                Match methodMatch = MethodRegex.Match(line);
                if (!methodMatch.Success)
                {
                    continue;
                }

                string methodName = methodMatch.Groups[3].Value.Trim();

                if (ControlKeywords.Contains(methodName))
                {
                    continue;
                }

                string returnType = methodMatch.Groups[2].Value.Trim();
                string parameters = methodMatch.Groups[4].Value.Trim();

                if (!IsEventHandlerLike(returnType, parameters))
                {
                    continue;
                }

                string? controlName;
                string? controlEventName;
                ParseControlAndEventFromMethodName(methodName, out controlName, out controlEventName);

                EventInfo handler = new EventInfo();
                handler.Kind = "HandlerMethod";
                handler.Name = methodName;
                handler.Namespace = currentNamespace;
                handler.DeclaringType = currentClass;
                handler.DeclaringFullName = BuildFullTypeName(currentNamespace, currentClass);
                handler.FilePath = Path.GetRelativePath(root, filePath);
                handler.LineNumber = i + 1;
                handler.HandlerSignature = parameters;
                handler.ControlName = controlName;
                handler.ControlEventName = controlEventName;

                events.Add(handler);
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
        // Helpers for handler detection
        // ─────────────────────────────────────────────────────────────
        private static bool IsEventHandlerLike(string returnType, string parameters)
        {
            // Must return void
            if (!string.Equals(returnType, "void", StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parameters))
            {
                return false;
            }

            string[] parts = parameters.Split(',');
            if (parts.Length < 2)
            {
                return false;
            }

            string first = parts[0].Trim();
            string second = parts[1].Trim();

            string firstType = ExtractTypeName(first);
            string secondType = ExtractTypeName(second);

            if (!string.Equals(firstType, "object", StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrEmpty(secondType))
            {
                return false;
            }

            // EventArgs, MouseEventArgs, FormClosingEventArgs, etc.
            if (!secondType.EndsWith("EventArgs", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static string ExtractTypeName(string parameter)
        {
            // remove ref/out/in/this
            string cleaned = parameter
                .Replace("ref ", string.Empty)
                .Replace("out ", string.Empty)
                .Replace("in ", string.Empty)
                .Replace("this ", string.Empty)
                .Trim();

            // type is first token before parameter name
            string[] tokens = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return string.Empty;
            }

            // handle "System.EventArgs e"
            return tokens[0].Contains(".")
                ? tokens[0].Substring(tokens[0].LastIndexOf('.') + 1)
                : tokens[0];
        }

        private static void ParseControlAndEventFromMethodName(string methodName, out string? controlName, out string? eventName)
        {
            controlName = null;
            eventName = null;

            int idx = methodName.LastIndexOf('_');
            if (idx <= 0 || idx >= methodName.Length - 1)
            {
                return;
            }

            controlName = methodName.Substring(0, idx);
            eventName = methodName.Substring(idx + 1);
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string root, List<EventInfo> events)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Events.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(events, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<EventInfo> events)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Events.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Event List");
            sb.AppendLine();
            sb.AppendLine($"Total events: {events.Count}");
            sb.AppendLine();

            foreach (EventInfo e in events)
            {
                sb.Append("- ");
                sb.Append(e.Kind);
                sb.Append(": ");

                if (!string.IsNullOrWhiteSpace(e.DeclaringFullName))
                {
                    sb.Append(e.DeclaringFullName);
                    sb.Append(".");
                }

                sb.Append(e.Name);

                if (!string.IsNullOrWhiteSpace(e.EventType))
                {
                    sb.Append(" : ");
                    sb.Append(e.EventType);
                }

                if (!string.IsNullOrWhiteSpace(e.ControlName) || !string.IsNullOrWhiteSpace(e.ControlEventName))
                {
                    sb.Append(" [");
                    sb.Append(e.ControlName);
                    sb.Append(".");
                    sb.Append(e.ControlEventName);
                    sb.Append("]");
                }

                sb.Append("  (");
                sb.Append(e.FilePath);
                sb.Append(":");
                sb.Append(e.LineNumber);
                sb.AppendLine(")");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
