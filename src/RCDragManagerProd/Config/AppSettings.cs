using System;
using System.IO;
using System.Text.Json;

namespace RCDragManagerProd.Config
{
    public static class AppSettings
    {
        private sealed class Model
        {
            // Debug builds: ON by default; Release builds: OFF by default
            public bool EnableLogging { get; set; } =
#if DEBUG
                true;
#else
                false;
#endif
            public bool LiveBroadcastEnabled { get; set; } = false;
            public bool LiveBroadcastDebugLogging { get; set; } = false;
        }

        private static readonly string AppFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "RC_Drag_Manager");

        private static readonly string FilePath = Path.Combine(AppFolder, "appsettings.json");

        private static Model _model = new Model();

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);

                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    _model = JsonSerializer.Deserialize<Model>(json) ?? new Model();
                }
                else
                {
                    // Persist the build-default the very first time
                    Save();
                }
            }
            catch
            {
                _model = new Model(); // fail-safe defaults
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { /* ignore */ }
        }

        public static bool EnableLogging
        {
            get => _model.EnableLogging;
            set { _model.EnableLogging = value; Save(); }
        }

        public static bool LiveBroadcastEnabled
        {
            get => _model.LiveBroadcastEnabled;
            set { _model.LiveBroadcastEnabled = value; Save(); }
        }

        public static bool LiveBroadcastDebugLogging
        {
            get => _model.LiveBroadcastDebugLogging;
            set { _model.LiveBroadcastDebugLogging = value; Save(); }
        }

        public static string LogFilePath
        {
            get
            {
                Directory.CreateDirectory(AppFolder);
                return Path.Combine(AppFolder, "app.log");
            }
        }
    }
}
