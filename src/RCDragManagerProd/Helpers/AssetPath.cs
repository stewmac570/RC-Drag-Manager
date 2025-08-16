using System;
using System.IO;
using RCDragManagerProd.Logging;
using System.Linq;

using RCDragManagerProd.Domain;        // only if a helper touches models


namespace RCDragManagerProd.Helpers.Helpers
{
    internal static class AssetPath
    {
        public static string Get(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
            Logger.Log($"[Assets] Resolve: {fileName} -> {path}");
            return path;
        }
    }
}
