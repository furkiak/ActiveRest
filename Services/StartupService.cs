using Microsoft.Win32;
using System;
using System.Runtime.Versioning;

namespace ActiveRest.Services
{
    /// <summary>Registers the app in the per-user Windows "Run" key so it starts quietly (tray only) at sign-in.</summary>
    public sealed class StartupService
    {
        public const string MinimizedArgument = "--minimized";
        private const string AppName = "ActiveRest";
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public bool IsSupported => OperatingSystem.IsWindows();

        public bool IsAutoStartEnabled()
        {
            if (!OperatingSystem.IsWindows()) return false;
            try { return ReadEnabled(); }
            catch (Exception ex)
            {
                Logger.Warn("Cannot read the auto-start setting", ex);
                return false;
            }
        }

        public void SetAutoStart(bool enable)
        {
            if (!OperatingSystem.IsWindows()) return;
            try { Write(enable); }
            catch (Exception ex) { Logger.Error("Cannot change the auto-start setting", ex); }
        }

        /// <summary>
        /// Keeps an existing auto-start entry pointing at the running executable, so moving or updating the app
        /// does not leave a dead entry behind. Does nothing when auto-start is off.
        /// </summary>
        public void RepairIfMoved()
        {
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                string expected = BuildCommand(CurrentExecutable());
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key?.GetValue(AppName) is string registered && !string.Equals(registered, expected, StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(AppName, expected);
                    Logger.Info("Auto-start entry updated to the current executable path.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Cannot check the auto-start entry", ex);
            }
        }

        internal static string BuildCommand(string exe) => $"\"{exe}\" {MinimizedArgument}";

        private static string CurrentExecutable() =>
            Environment.ProcessPath ?? System.IO.Path.Combine(AppContext.BaseDirectory, "ActiveRest.exe");

        [SupportedOSPlatform("windows")]
        private static bool ReadEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }

        [SupportedOSPlatform("windows")]
        private static void Write(bool enable)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enable)
                key.SetValue(AppName, BuildCommand(CurrentExecutable()));
            else
                key.DeleteValue(AppName, false);
        }
    }
}
