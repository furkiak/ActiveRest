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
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable) key.SetValue(AppName, $"\"{_exePath}\"");
                    else key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex) { Console.WriteLine("Error-5: " + ex.Message); }
        }

        public bool IsAutoStartEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
    }
}