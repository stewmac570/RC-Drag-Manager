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

            // Per-query/per-push Logger.Debug chatter; off by default so race-day
            // logs stay small even with EnableLogging on.
            public bool VerboseLogging { get; set; } = false;

            // UI theme for the WPF app: "Dark" (default) or "Light".
            public string Theme { get; set; } = "Dark";

            // X-API-KEY for the live scoreboard server. Deliberately NOT in
            // App.config: the repo is public and a committed key was exposed
            // (#377). Set once via the Settings dialog; empty disables auth'd
            // live calls.
            public string ApiKey { get; set; } = "";
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

            MigrateApiKeyFromExeConfig();
        }

        // One-time adoption for installs that predate #377, where the key lived in
        // the exe.config. Copies it into appsettings.json so removing it from the
        // repo (and future installers) doesn't break an already-configured machine.
        private static void MigrateApiKeyFromExeConfig()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_model.ApiKey)) return;
                var legacy = System.Configuration.ConfigurationManager.AppSettings["ApiKey"];
                if (string.IsNullOrWhiteSpace(legacy)) return;
                _model.ApiKey = legacy;
                Save();
            }
            catch { /* no exe.config access — nothing to migrate */ }
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

        public static bool VerboseLogging
        {
            get => _model.VerboseLogging;
            set { _model.VerboseLogging = value; Save(); }
        }

        public static string ApiKey
        {
            get => _model.ApiKey ?? "";
            set { _model.ApiKey = (value ?? "").Trim(); Save(); }
        }

        public static string Theme
        {
            get => string.IsNullOrWhiteSpace(_model.Theme) ? "Dark" : _model.Theme;
            set { _model.Theme = value; Save(); }
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
