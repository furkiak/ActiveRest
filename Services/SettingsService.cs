using Microsoft.Win32;
using System;
using System.IO;

namespace ActiveRest.Services
{
    public class SettingsService
    {
        private const string AppName = "ActiveRest";
        private readonly StorageProfile _profile;
        private readonly string _exePath = Path.Combine(AppContext.BaseDirectory, "ActiveRest.exe"); 
        public SettingsService(StorageProfile? profile = null) => _profile = profile ?? AppStorage.Profile;
        public bool CanSetAutoStart => !_profile.IsSeparate && OperatingSystem.IsWindows();
        public void SetAutoStart(bool enable)
        {
            if (_profile.IsSeparate) return;
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Auto-start requires Windows.");
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
                ?? throw new InvalidOperationException("Could not open the current user's startup settings.");
            if (enable) key.SetValue(AppName, $"\"{_exePath}\"");
            else key.DeleteValue(AppName, false);
        }

        public bool IsAutoStartEnabled()
        {
            if (_profile.IsSeparate) return false;
            if (!OperatingSystem.IsWindows()) return false;
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
    }
}
