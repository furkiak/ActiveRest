using System;
using System.Diagnostics;
using System.IO;

namespace ActiveRest.Services
{
    /// <summary>Opens files and folders in the platform file manager. Failures are logged, never thrown.</summary>
    public static class ShellHelper
    {
        public static void RevealInFolder(string path)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                else if (OperatingSystem.IsMacOS())
                    Process.Start("open", new[] { "-R", path });
                else
                    OpenFolder(Path.GetDirectoryName(path) ?? path);
            }
            catch (Exception ex)
            {
                Logger.Warn("Cannot reveal file", ex);
            }
        }

        public static void OpenFolder(string folder)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
                else if (OperatingSystem.IsMacOS())
                    Process.Start("open", folder);
                else
                    Process.Start("xdg-open", folder);
            }
            catch (Exception ex)
            {
                Logger.Warn("Cannot open folder", ex);
            }
        }
    }
}
