using System;
using System.Configuration;
using System.IO;

namespace RCDragManagerProd.Config
{
    public static class AppSettings
    {
        public static bool EnableLogging =>
            bool.TryParse(ConfigurationManager.AppSettings["EnableLogging"], out var flag) && flag;

        public static string LogFilePath
        {
            get
            {
                string baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RC-Drag_Manager");

                if (!Directory.Exists(baseFolder))
                    Directory.CreateDirectory(baseFolder);

                return Path.Combine(baseFolder, "log.txt");
            }
        }
    }
}
