using System;
using Avalonia;
using ActiveRest.Services;
using System.Runtime.InteropServices;

namespace ActiveRest
{
    internal sealed class Program
    { 
        [STAThread]
        public static void Main(string[] args)
        {
            try { AppStorage.Configure(args); }
            catch (Exception ex)
            {
                if (OperatingSystem.IsWindows()) MessageBox(IntPtr.Zero, ex.Message, "ActiveRest could not open this profile", 0x10);
                else Console.Error.WriteLine(ex.Message);
                return;
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr window, string text, string title, uint type);
         
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
