using System;
using System.Configuration;
using System.IO;

namespace RCDragManagerProd
{
    public static class Logger
    {
        private static readonly bool _enabled;
        private static readonly string _logPath;

        static Logger()
        {
            _enabled = bool.TryParse(ConfigurationManager.AppSettings["EnableLogging"], out bool flag) && flag;

            string rawPath = ConfigurationManager.AppSettings["LogFilePath"] ?? "logs/rc_drag_log.txt";
            string expandedPath = Environment.ExpandEnvironmentVariables(rawPath);

            // Manual expansion for %APPDATA%\...
            if (expandedPath.StartsWith("%APPDATA%", StringComparison.OrdinalIgnoreCase))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                expandedPath = Path.Combine(appData, expandedPath.Substring(10).TrimStart('\\', '/'));
            }

            _logPath = Path.GetFullPath(expandedPath);

            if (_enabled)
            {
                string dir = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        public static void Log(string message)
        {
            if (!_enabled) return;

            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                // Silent fail
            }

        }
    }
}
