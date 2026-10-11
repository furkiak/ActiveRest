using ActiveRest.Services;
using Avalonia;
using System;
using System.Threading.Tasks;

namespace ActiveRest
{
    internal sealed class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Logger.Error("Unhandled exception", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Logger.Error("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            using var instance = new SingleInstanceGuard();
            if (!instance.IsPrimary)
            {
                SingleInstanceGuard.SignalPrimary();
                return 0;
            }

            instance.StartListening();
            App.SingleInstance = instance;
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
