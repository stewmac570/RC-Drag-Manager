using System;
using RCDragManagerProd.Config;

namespace RCDragManagerProd.Logging
{
    /// <summary>
    /// Static logging facade. Writes go through one shared <see cref="LogWriter"/>
    /// (single file handle, lock-guarded) instead of File.AppendAllText per line —
    /// concurrent writers (live-feed drain workers, dial-in poll timer, UI thread)
    /// used to race the open-append-close and silently lose lines (#383).
    /// </summary>
    public static class Logger
    {
        private static readonly LogWriter _writer = new LogWriter(AppSettings.LogFilePath);

        private static bool Enabled => AppSettings.EnableLogging; // reads live every call

        public static void Log(string message)
        {
            if (Enabled) _writer.WriteLine(message);
        }

        /// <summary>Per-query / per-push chatter. Only written when
        /// <see cref="AppSettings.VerboseLogging"/> is also on, keeping race-day
        /// logs readable and cheap.</summary>
        public static void Debug(string message)
        {
            if (Enabled && AppSettings.VerboseLogging) _writer.WriteLine(message);
        }

        public static void LogError(string message) => Log("[ERROR] " + message);
        public static void LogFatal(string message) => Log("[FATAL] " + message);
    }
}
