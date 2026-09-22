using Microsoft.Win32;
using System;
using System.IO;

namespace ActiveRest.Services
{
    public class SettingsService
    {
        private const string AppName = "ActiveRest";
        private readonly string _exePath = Path.Combine(AppContext.BaseDirectory, "ActiveRest.exe"); 
        public void SetAutoStart(bool enable)
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Auto-start requires Windows.");
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
                ?? throw new InvalidOperationException("Could not open the current user's startup settings.");
            if (enable) key.SetValue(AppName, $"\"{_exePath}\"");
            else key.DeleteValue(AppName, false);
        }

        public bool IsAutoStartEnabled()
        {
            if (!OperatingSystem.IsWindows()) return false;
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
    }
}
