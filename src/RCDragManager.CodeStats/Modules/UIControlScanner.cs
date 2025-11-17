using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class UIControlScanner
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

        // partial class Form1 : Form
        private static readonly Regex ClassRegex =
            new Regex(@"^\s*(public|internal|protected|private|sealed|abstract|static|partial|\s)*\s*class\s+([A-Za-z0-9_]+)",
                      RegexOptions.Compiled);

        // Inside InitializeComponent:
        // this.btnSave = new System.Windows.Forms.Button();
        // this.txtName  = new TextBox();
        private static readonly Regex ControlCreateRegex =
            new Regex(
                @"^\s*this\.(?<name>[A-Za-z0-9_]+)\s*=\s*new\s+(?<type>[A-Za-z0-9_.]+)\s*\(",
                RegexOptions.Compiled);

        // Parent/child relationships:
        // this.panelMain.Controls.Add(this.btnSave);
        private static readonly Regex ControlsAddRegex =
            new Regex(
                @"^\s*this\.(?<parent>[A-Za-z0-9_]+)\.Controls\.Add\(\s*this\.(?<child>[A-Za-z0-9_]+)\s*\)\s*;",
                RegexOptions.Compiled);

        // Event hookup:
        // this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        // this.btnSave.Click += this.btnSave_Click;
        // this.btnSave.Click += btnSave_Click;
        private static readonly Regex EventHookRegex =
            new Regex(
                @"^\s*this\.(?<control>[A-Za-z0-9_]+)\.(?<event>[A-Za-z0-9_]+)\s*\+=\s*(new\s+[A-Za-z0-9_.<>]+\s*)?\(?\s*(this\.)?(?<handler>[A-Za-z0-9_]+)\s*\)?\s*;",
                RegexOptions.Compiled);

        public static List<UIControlInfo> Scan(string root)
        {
            Console.WriteLine("[SCAN] UI Control Scanner");

            List<UIControlInfo> allControls = new List<UIControlInfo>();
            int fileCount = 0;

            foreach (string file in EnumerateDesignerFiles(root))
            {
                fileCount++;
                ScanDesignerFile(root, file, allControls);
            }

            Console.WriteLine($"[SCAN]   Designer files scanned : {fileCount}");
            Console.WriteLine($"[SCAN]   UI controls found      : {allControls.Count}");

            WriteJson(root, allControls);
            WriteMarkdown(root, allControls);

            return allControls;
        }

        // ─────────────────────────────────────────────────────────────
        // File enumeration – only *.Designer.cs
        // ─────────────────────────────────────────────────────────────
        private static IEnumerable<string> EnumerateDesignerFiles(string root)
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
                    files = Directory.GetFiles(dir, "*.Designer.cs");
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
        // Per-designer parsing
        // ─────────────────────────────────────────────────────────────
        private static void ScanDesignerFile(string root, string filePath, List<UIControlInfo> allControls)
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
            string? currentFullType = null;

            Dictionary<string, UIControlInfo> controlsByName =
                new Dictionary<string, UIControlInfo>(StringComparer.Ordinal);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                Match nsMatch = NamespaceRegex.Match(line);
                if (nsMatch.Success)
                {
                    currentNamespace = nsMatch.Groups[1].Value.Trim();
                    currentFullType = null; // will recompute when class is seen
                    continue;
                }

                Match classMatch = ClassRegex.Match(line);
                if (classMatch.Success)
                {
                    currentClass = classMatch.Groups[2].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(currentNamespace))
                    {
                        currentFullType = currentNamespace + "." + currentClass;
                    }
                    else
                    {
                        currentFullType = currentClass;
                    }

                    continue;
                }

                // Control instantiation
                Match createMatch = ControlCreateRegex.Match(line);
                if (createMatch.Success)
                {
                    string controlName = createMatch.Groups["name"].Value.Trim();
                    string typeFull = createMatch.Groups["type"].Value.Trim();

                    UIControlInfo info;

                    if (!controlsByName.TryGetValue(controlName, out info))
                    {
                        info = new UIControlInfo();
                        controlsByName[controlName] = info;
                    }

                    info.Name = controlName;
                    info.TypeFullName = typeFull;
                    info.TypeShortName = GetShortTypeName(typeFull);
                    info.FilePath = Path.GetRelativePath(root, filePath);

                    if (info.DeclarationLine == 0)
                    {
                        info.DeclarationLine = i + 1;
                    }

                    info.Namespace = currentNamespace;
                    info.DeclaringType = currentClass;
                    info.DeclaringFullName = currentFullType;

                    continue;
                }

                // Parent/child addition
                Match addMatch = ControlsAddRegex.Match(line);
                if (addMatch.Success)
                {
                    string parentName = addMatch.Groups["parent"].Value.Trim();
                    string childName = addMatch.Groups["child"].Value.Trim();

                    UIControlInfo childInfo;

                    if (!controlsByName.TryGetValue(childName, out childInfo))
                    {
                        childInfo = new UIControlInfo();
                        controlsByName[childName] = childInfo;
                        childInfo.Name = childName;
                        childInfo.FilePath = Path.GetRelativePath(root, filePath);
                        childInfo.DeclarationLine = childInfo.DeclarationLine == 0 ? i + 1 : childInfo.DeclarationLine;
                    }

                    childInfo.ParentName = parentName;
                    childInfo.Namespace = currentNamespace;
                    childInfo.DeclaringType = currentClass;
                    childInfo.DeclaringFullName = currentFullType;

                    continue;
                }

                // Event hookups
                Match eventMatch = EventHookRegex.Match(line);
                if (eventMatch.Success)
                {
                    string controlName = eventMatch.Groups["control"].Value.Trim();
                    string eventName = eventMatch.Groups["event"].Value.Trim();
                    string handlerName = eventMatch.Groups["handler"].Value.Trim();

                    UIControlInfo control;

                    if (!controlsByName.TryGetValue(controlName, out control))
                    {
                        control = new UIControlInfo();
                        controlsByName[controlName] = control;
                        control.Name = controlName;
                        control.FilePath = Path.GetRelativePath(root, filePath);
                        control.DeclarationLine = control.DeclarationLine == 0 ? i + 1 : control.DeclarationLine;
                    }

                    control.Namespace = currentNamespace;
                    control.DeclaringType = currentClass;
                    control.DeclaringFullName = currentFullType;

                    UIControlEventBinding binding = new UIControlEventBinding();
                    binding.EventName = eventName;
                    binding.HandlerName = handlerName;
                    binding.LineNumber = i + 1;

                    control.Events.Add(binding);

                    continue;
                }
            }

            foreach (UIControlInfo info in controlsByName.Values)
            {
                allControls.Add(info);
            }
        }

        private static string GetShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return string.Empty;
            }

            int idx = fullTypeName.LastIndexOf('.');
            if (idx < 0 || idx >= fullTypeName.Length - 1)
            {
                return fullTypeName;
            }

            return fullTypeName.Substring(idx + 1);
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string root, List<UIControlInfo> controls)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "UIControls.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(controls, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<UIControlInfo> controls)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "UIControls.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# UI Controls");
            sb.AppendLine();
            sb.AppendLine($"Total controls: {controls.Count}");
            sb.AppendLine();

            // Group by declaring type (form/user control)
            Dictionary<string, List<UIControlInfo>> byForm =
                new Dictionary<string, List<UIControlInfo>>(StringComparer.Ordinal);

            foreach (UIControlInfo c in controls)
            {
                string key = c.DeclaringFullName ?? c.DeclaringType ?? "<Unknown>";

                if (!byForm.TryGetValue(key, out List<UIControlInfo> list))
                {
                    list = new List<UIControlInfo>();
                    byForm[key] = list;
                }

                list.Add(c);
            }

            foreach (KeyValuePair<string, List<UIControlInfo>> kvp in byForm)
            {
                sb.AppendLine("## " + kvp.Key);
                sb.AppendLine();

                List<UIControlInfo> list = kvp.Value;
                list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

                foreach (UIControlInfo c in list)
                {
                    sb.Append("- ");
                    sb.Append(c.Name);
                    sb.Append(" : ");
                    sb.Append(c.TypeShortName);

                    if (!string.IsNullOrWhiteSpace(c.ParentName))
                    {
                        sb.Append("  (parent: ");
                        sb.Append(c.ParentName);
                        sb.Append(")");
                    }

                    sb.Append("  [");
                    sb.Append(c.FilePath);
                    sb.Append(":");
                    sb.Append(c.DeclarationLine);
                    sb.Append("]");

                    if (c.Events.Count > 0)
                    {
                        sb.Append("  Events: ");

                        for (int i = 0; i < c.Events.Count; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }

                            UIControlEventBinding ev = c.Events[i];
                            sb.Append(ev.EventName);
                            sb.Append("→");
                            sb.Append(ev.HandlerName);
                        }
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
