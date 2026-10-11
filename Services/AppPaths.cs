using System;
using System.IO;

namespace ActiveRest.Services
{
    public static class AppPaths
    {
        /// <summary>Folder that holds stats, settings and the log. Can be redirected with ACTIVEREST_DATA_DIR.</summary>
        public static string DataFolder { get; } = ResolveDataFolder();

        private static string ResolveDataFolder()
        {
            string? overridePath = Environment.GetEnvironmentVariable("ACTIVEREST_DATA_DIR");
            string folder = string.IsNullOrWhiteSpace(overridePath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActiveRest")
                : overridePath;

            try { Directory.CreateDirectory(folder); }
            catch { /* reported lazily by the first write */ }
            return folder;
        }
    }
}
