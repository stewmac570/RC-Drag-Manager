using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class UIEventMapExporter
    {
        public class UIEventLink
        {
            public string FormFullName { get; set; } = string.Empty;

            public string ControlName { get; set; } = string.Empty;

            public string? ControlType { get; set; }

            public string EventName { get; set; } = string.Empty;

            public string HandlerName { get; set; } = string.Empty;

            public string? HandlerFullName { get; set; }

            public string DesignerFilePath { get; set; } = string.Empty;

            public int DesignerLineNumber { get; set; }

            public string? HandlerFilePath { get; set; }

            public int HandlerLineNumber { get; set; }
        }

        public static void Export(
            string root,
            List<UIControlInfo> controls,
            List<EventInfo> events)
        {
            Console.WriteLine("[OUT] UI Event Map Exporter");

            Dictionary<string, EventInfo> handlersByName =
                new Dictionary<string, EventInfo>(StringComparer.Ordinal);

            for (int i = 0; i < events.Count; i++)
            {
                EventInfo e = events[i];

                if (e.Kind != "HandlerMethod")
                {
                    continue;
                }

                if (!handlersByName.ContainsKey(e.Name))
                {
                    handlersByName[e.Name] = e;
                }
            }

            List<UIEventLink> links = new List<UIEventLink>();

            for (int i = 0; i < controls.Count; i++)
            {
                UIControlInfo c = controls[i];

                string formFull = c.DeclaringFullName ?? c.DeclaringType ?? "<UnknownForm>";

                for (int j = 0; j < c.Events.Count; j++)
                {
                    UIControlEventBinding binding = c.Events[j];

                    UIEventLink link = new UIEventLink();
                    link.FormFullName = formFull;
                    link.ControlName = c.Name;
                    link.ControlType = c.TypeShortName;
                    link.EventName = binding.EventName;
                    link.HandlerName = binding.HandlerName;
                    link.DesignerFilePath = c.FilePath;
                    link.DesignerLineNumber = binding.LineNumber;

                    if (handlersByName.TryGetValue(binding.HandlerName, out EventInfo handler))
                    {
                        link.HandlerFullName = handler.DeclaringFullName + "." + handler.Name;
                        link.HandlerFilePath = handler.FilePath;
                        link.HandlerLineNumber = handler.LineNumber;
                    }

                    links.Add(link);
                }
            }

            WriteJson(root, links);
            WriteMarkdown(root, links);
        }

        private static void WriteJson(string root, List<UIEventLink> links)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "UIEventMap.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(links, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string root, List<UIEventLink> links)
        {
            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "UIEventMap.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# UI Event Map");
            sb.AppendLine();
            sb.AppendLine("Total bindings: " + links.Count);
            sb.AppendLine();

            Dictionary<string, List<UIEventLink>> byForm =
                new Dictionary<string, List<UIEventLink>>(StringComparer.Ordinal);

            for (int i = 0; i < links.Count; i++)
            {
                UIEventLink link = links[i];

                if (!byForm.TryGetValue(link.FormFullName, out List<UIEventLink> list))
                {
                    list = new List<UIEventLink>();
                    byForm[link.FormFullName] = list;
                }

                list.Add(link);
            }

            foreach (KeyValuePair<string, List<UIEventLink>> kvp in byForm)
            {
                sb.AppendLine("## " + kvp.Key);
                sb.AppendLine();

                List<UIEventLink> list = kvp.Value;
                list.Sort((a, b) =>
                {
                    int cmp = string.CompareOrdinal(a.ControlName, b.ControlName);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    return string.CompareOrdinal(a.EventName, b.EventName);
                });

                for (int i = 0; i < list.Count; i++)
                {
                    UIEventLink link = list[i];

                    sb.Append("- ");
                    sb.Append(link.ControlName);
                    sb.Append(" (");
                    sb.Append(link.ControlType);
                    sb.Append(") ");
                    sb.Append(link.EventName);
                    sb.Append(" → ");
                    sb.Append(link.HandlerName);

                    if (!string.IsNullOrWhiteSpace(link.HandlerFilePath))
                    {
                        sb.Append("  [");
                        sb.Append(link.HandlerFilePath);
                        sb.Append(":");
                        sb.Append(link.HandlerLineNumber);
                        sb.Append("]");
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
