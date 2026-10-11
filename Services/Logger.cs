using System;
using System.Diagnostics;
using System.IO;

namespace ActiveRest.Services
{
    /// <summary>Minimal file logger. Never throws; the log is trimmed when it grows past 256 KB.</summary>
    public static class Logger
    {
        private const long MaxBytes = 256 * 1024;
        private static readonly object Gate = new();
        private static string LogPath => Path.Combine(AppPaths.DataFolder, "activerest.log");

        public static void Info(string message) => Write("INFO", message, null);
        public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception? ex)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{(ex != null ? " :: " + ex.GetType().Name + ": " + ex.Message : string.Empty)}";
            Debug.WriteLine(line);
            try
            {
                lock (Gate)
                {
                    var info = new FileInfo(LogPath);
                    if (info.Exists && info.Length > MaxBytes)
                        File.Move(LogPath, LogPath + ".old", overwrite: true);
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never take the app down.
            }
        }
    }
}
