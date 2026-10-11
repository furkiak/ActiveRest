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
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private static double _scale = 1;
    private static bool _matte;

    private static int Main(string[] args)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());   // surface binding/resource warnings
        _matte = args.Contains("--matte");
        int scaleArg = Array.IndexOf(args, "--scale");
        if (scaleArg >= 0 && scaleArg + 1 < args.Length) _scale = double.Parse(args[scaleArg + 1], System.Globalization.CultureInfo.InvariantCulture);
        _outDir = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--") && !double.TryParse(a, out _)) ?? "screenshots");
        Directory.CreateDirectory(_outDir);

        string dataDir = Path.Combine(Path.GetTempPath(), "activerest-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);
        Environment.SetEnvironmentVariable("ACTIVEREST_DATA_DIR", dataDir);

        AppBuilder.Configure<ActiveRest.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .LogToTrace(Avalonia.Logging.LogEventLevel.Warning)
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

        // Sample exports, so the report layout can be reviewed as well.
        vm.Period = PeriodKind.Week;
        vm.Anchor = DateTime.Today.AddDays(-3);
        foreach (string lang in new[] { "en", "tr" })
        {
            Loc.Instance.SetLanguage(lang);
            vm.RefreshNow();
            var report = vm.BuildReport();
            services.Pdf.Export(report, Path.Combine(_outDir, $"report-{lang}.pdf"), Loc.Instance);
            services.Csv.Export(report, Path.Combine(_outDir, $"report-{lang}.csv"), Loc.Instance);
        }

        if (args.Contains("--selftest")) SelfTest(services, vm);

        Console.WriteLine("Screenshots written to " + _outDir);
        return Environment.ExitCode;
    }

    private static void SelfTest(AppServices services, MainWindowViewModel vm)
    {
        void Check(bool ok, string what) { Console.WriteLine((ok ? "  PASS " : "  FAIL ") + what); if (!ok) Environment.ExitCode = 1; }

        Loc.Instance.SetLanguage("en");
        vm.Period = PeriodKind.Day; vm.Anchor = DateTime.Today;
        var main = NewMain(vm, services);
        main.Show();
        ((MainWindow)main).ShowFromTray();
        Pump(0.3);

        // calendar flyout
        var dateButton = main.FindControl<Button>("DateButton")!;
        dateButton.Flyout!.ShowAt(dateButton);
        Pump(0.4);
        Check(dateButton.Flyout.IsOpen, "calendar flyout opens");
        dateButton.Flyout.Hide();
        Pump(0.2);
        Check(!dateButton.Flyout.IsOpen, "calendar flyout closes");

        // navigation
        Check(!vm.CanGoNext && !vm.NextCommand.CanExecute(null), "cannot navigate into the future");
        vm.PreviousCommand.Execute(null);
        Check(vm.Anchor == DateTime.Today.AddDays(-1) && vm.RangeTitle == "Yesterday", "previous day -> Yesterday");
        Check(vm.CanGoNext && !vm.IsCurrentPeriod, "can go forward again, no longer current");
        vm.NextCommand.Execute(null);
        Check(vm.IsCurrentPeriod && vm.RangeTitle == "Today", "next day -> Today");
        vm.PeriodIndex = 1; vm.PreviousCommand.Execute(null);
        Check(vm.RangeTitle == "Last week", "week view: previous -> Last week");
        vm.PeriodIndex = 2; vm.GoToTodayCommand.Execute(null);
        Check(vm.RangeTitle == "This month" && vm.ChartBars.Count >= 28, "month view builds >= 28 bars");
        vm.PeriodIndex = 3;
        Check(vm.ChartBars.Count == 12, "year view builds 12 bars");
        vm.SelectedDate = DateTime.Today.AddDays(-40);
        Check(vm.Anchor == DateTime.Today.AddDays(-40), "calendar selection moves the anchor");
        vm.SelectedDate = DateTime.Today.AddDays(5);
        Check(vm.Anchor == DateTime.Today, "future dates are clamped to today");
        vm.PeriodIndex = 0;

        // pause
        vm.TogglePauseCommand.Execute(null);
        Check(vm.IsPaused && vm.StatusText == "Tracking paused", "pause updates status");
        vm.TogglePauseCommand.Execute(null);
        Check(!vm.IsPaused, "resume");

        // language switch refreshes derived text
        Loc.Instance.SetLanguage("tr");
        Check(vm.RangeTitle == "Bugün" && vm.ChartTitle == "Saatlik odak", "language switch re-renders view-model text");
        Loc.Instance.SetLanguage("en");

        // export through the view model
        var details = new DetailsViewModel(vm, services.Pdf, services.Csv);
        string pdf = Path.Combine(_outDir, "selftest.pdf");
        var task = details.ExportAsync(ActiveRest.ViewModels.ExportFormat.Pdf, pdf);
        var sw = Stopwatch.StartNew();
        while (!task.IsCompleted && sw.Elapsed.TotalSeconds < 20) Pump(0.05);
        Check(task.IsCompletedSuccessfully && File.Exists(pdf) && !details.IsError && details.CanReveal, "PDF export through the details view model");

        // settings view model writes through the store
        var sv = new SettingsViewModel(services.Settings, services.Startup);
        sv.GoalHours = 4; sv.EyeInterval = 45; sv.IdleMinutes = 5; sv.EyeEnabled = false;
        var cur = services.Settings.Current;
        Check(cur.DailyFocusGoalMinutes == 240 && cur.EyeRestIntervalMinutes == 45 && cur.IdleThresholdMinutes == 5 && !cur.IsEyeRestEnabled, "settings view model persists edits");
        Check(vm.GoalText.Contains("4h"), "goal change flows into the main view (" + vm.GoalText + ")");
        sv.Dispose();
        main.Close();
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
        if (_scale != 1) ScaleWindow(window);
        Pump(0.9);

        using (WriteableBitmap? frame = window.CaptureRenderedFrame())
            frame?.Save(Path.Combine(_outDir, name + ".png"));

        if (_matte)
        {
            // The captured bitmap has no alpha. Rendering over black and over white lets the documentation
            // script recover true transparency (rounded corners and shadows) with a difference matte.
            foreach (var (suffix, brush) in new[] { ("black", Brushes.Black), ("white", Brushes.White) })
            {
                window.Background = brush;
                Pump(0.15);
                using WriteableBitmap? matte = window.CaptureRenderedFrame();
                matte?.Save(Path.Combine(_outDir, $"{name}.{suffix}.png"));
            }
        }
        window.Close();
        Console.WriteLine("  " + name);
    }

    /// <summary>Renders the window at a higher pixel density by scaling its layout (crisp vector output for documentation).</summary>
    private static void ScaleWindow(Window window)
    {
        double width = window.Width, height = double.IsNaN(window.Height) ? double.NaN : window.Height;
        var child = window.Content as Control;
        window.Content = null;
        bool autoHeight = window.SizeToContent != SizeToContent.Manual;
        if (autoHeight)
        {
            // Auto-sized windows (the toast) take their width from the window; pin it on the content instead.
            if (child != null) child.Width = width - 32;   // minus the root's 16 DIP margins
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
        else
        {
            window.Width = width * _scale;
            window.Height = height * _scale;
        }
        window.Content = new LayoutTransformControl { LayoutTransform = new ScaleTransform(_scale, _scale), Child = child };
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
