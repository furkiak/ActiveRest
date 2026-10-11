// Renders the real windows with seeded demo data through Avalonia's headless Skia platform
// (no display needed) and writes PNGs: dotnet run --project tools/Screenshots -- <output-dir>
using ActiveRest.Localization;
using ActiveRest.Models;
using ActiveRest.Services;
using ActiveRest.ViewModels;
using ActiveRest.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Screenshots;

internal sealed class DemoTracker : IActivityTracker
{
    public double GetIdleTimeSeconds() => 0;
    public bool IsMediaPlaying() => false;
    public string GetActiveAppName() => "Code";
}

internal static class Program
{
    private static string _outDir = "screenshots";

    private static int Main(string[] args)
    {
        _outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "screenshots");
        Directory.CreateDirectory(_outDir);

        string dataDir = Path.Combine(Path.GetTempPath(), "activerest-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);
        Environment.SetEnvironmentVariable("ACTIVEREST_DATA_DIR", dataDir);

        AppBuilder.Configure<ActiveRest.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

        var seedData = new DataManager(dataDir);
        Seed(seedData, DateTime.Today);
        seedData.Flush();

        var services = new AppServices(new DemoTracker(), dataDir);
        Thread.Sleep(30);
        services.Engine.Tick();   // classify the demo user as "focused" so the live status looks right
        var vm = new MainWindowViewModel(services.Engine, services.Data, services.Settings);

        foreach (var (lang, theme) in new[] { ("en", ThemeVariant.Dark), ("en", ThemeVariant.Light), ("tr", ThemeVariant.Dark) })
        {
            string tag = $"{lang}-{(theme == ThemeVariant.Dark ? "dark" : "light")}";
            Loc.Instance.SetLanguage(lang);
            Application.Current!.RequestedThemeVariant = theme;

            vm.Period = PeriodKind.Day;
            vm.Anchor = DateTime.Today;
            Capture($"main-day-{tag}", () => NewMain(vm, services), vm);

            if (lang == "en")
            {
                vm.Period = PeriodKind.Week;
                Capture($"main-week-{tag}", () => NewMain(vm, services), vm);
                vm.Period = PeriodKind.Month;
                Capture($"main-month-{tag}", () => NewMain(vm, services), vm);
                vm.Period = PeriodKind.Year;
                Capture($"main-year-{tag}", () => NewMain(vm, services), vm);
            }

            vm.Period = PeriodKind.Week;
            vm.Anchor = DateTime.Today;
            Capture($"details-{tag}", () => new DetailsWindow
            {
                DataContext = new DetailsViewModel(vm, services.Pdf, services.Csv)
            }, vm);

            Capture($"settings-{tag}", () => new SettingsWindow
            {
                DataContext = new SettingsViewModel(services.Settings, services.Startup)
            }, vm);

            var toastVm = new ToastViewModel(20);
            toastVm.Advance(6.5);
            Capture($"toast-{tag}", () => new ToastWindow { DataContext = toastVm }, vm, isToast: true);
        }

        Console.WriteLine("Screenshots written to " + _outDir);
        return 0;
    }

    private static Window NewMain(MainWindowViewModel vm, AppServices services)
    {
        vm.IsWindowVisible = true;
        return new MainWindow { DataContext = vm, Services = services };
    }

    private static void Capture(string name, Func<Window> create, MainWindowViewModel vm, bool isToast = false)
    {
        var window = create();
        window.Show();
        vm.OnTick();
        if (window is MainWindow main) main.ShowFromTray();
        window.FindControl<Border>("Root")?.Classes.Set("hidden", false);
        Pump(0.9);

        using WriteableBitmap? frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(_outDir, name + ".png"));
        window.Close();
        Console.WriteLine("  " + name);
    }

    /// <summary>Lets timers, transitions and the renderer run for roughly the given time.</summary>
    private static void Pump(double seconds)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < seconds)
        {
            Thread.Sleep(16);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Seed(DataManager data, DateTime today)
    {
        var rnd = new Random(11);
        string[] apps = { "Code", "Chrome", "ms-teams", "Slack", "WINWORD", "Figma", "explorer", "Spotify" };
        double[] weights = { 0.34, 0.24, 0.14, 0.08, 0.07, 0.06, 0.04, 0.03 };

        for (int i = 120; i >= 0; i--)
        {
            DateTime date = today.AddDays(-i);
            bool weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if (weekend && rnd.NextDouble() < 0.6) continue;

            double focus = i == 0 ? 3 * 3600 + 42 * 60 : weekend ? 3600 * (0.5 + rnd.NextDouble() * 1.5) : 3600 * (3.4 + rnd.NextDouble() * 4.4);
            var day = new DailyStat(date)
            {
                TotalFocusedSeconds = focus,
                TotalRelaxedSeconds = focus * (0.05 + rnd.NextDouble() * 0.12),
                TotalBreakSeconds = focus * (0.1 + rnd.NextDouble() * 0.2),
                LongestFocusSeconds = Math.Min(focus, 1800 + rnd.NextDouble() * 5400)
            };

            double[] hourWeights = { 0, 0, 0, 0, 0, 0, 0, 0, 0.7, 1.0, 1.0, 0.85, 0.35, 0.8, 1.0, 1.0, 0.9, 0.6, 0.25, 0.1, 0.1, 0, 0, 0 };
            double total = 0;
            foreach (double w in hourWeights) total += w;
            for (int h = 0; h < 24; h++) day.HourlyFocusSeconds[h] = focus * hourWeights[h] / total;

            for (int a = 0; a < apps.Length; a++) day.AppUsageSeconds[apps[a]] = focus * weights[a];
            data.Upsert(day);
        }
    }
}
