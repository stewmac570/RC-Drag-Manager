using System;
using System.IO;
using RCDragManagerProd.Config;

namespace RCDragManagerProd.Logging
{
    public static class Logger
    {
        private static readonly string _logPath;

        static Logger()
        {
            // AppSettings.Load() will run in Program.Main; still ensure folder exists
            _logPath = AppSettings.LogFilePath;
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static bool Enabled => AppSettings.EnableLogging; // reads live every call

        public static void Log(string message)
        {
            if (!Enabled) return;
            try
            {
                File.AppendAllText(_logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
            catch { /* swallow */ }
        }

        public static void LogError(string message) => Log("[ERROR] " + message);
        public static void LogFatal(string message) => Log("[FATAL] " + message);
    }
}
