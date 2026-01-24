using System;
using System.IO;
using System.Text.Json;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class JsonExporter
    {
        public static void Export(ProjectMap map, string root)
        {
            Console.WriteLine("[OUT] JSON Exporter");

            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, "ProjectMap.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(map, options);
            File.WriteAllText(file, json);
        }
    }
}
