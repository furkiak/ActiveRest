using ActiveRest.Models;
using ActiveRest.Services;
using ActiveRest.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace ActiveRest.Tests;

[CollectionDefinition("Profile configuration", DisableParallelization = true)]
public sealed class ProfileConfigurationCollection;

[Collection("Profile configuration")]
public sealed class RuntimeProfileTests
{
    [Fact]
    public void Configured_profile_is_used_by_default_store_without_copying_normal_data()
    {
        using var folder = new TestFolder();
        var original = AppStorage.Profile;
        try
        {
            AppStorage.Configure(["--data-directory", Path.Combine(folder.Path, "profile")]);
            var data = new DataManager();
            Assert.Equal(AppStorage.Profile.Directory, data.DataFolder);
            Assert.True(data.Profile.IsSeparate);
            Assert.Empty(data.LoadAllData());
            data.SaveSettings(new AppSettings { EyeRestIntervalMinutes = 5 });
            data.SaveOrUpdateToday(new DailyStat { TotalFocusedSeconds = 3 });
            Assert.True(File.Exists(Path.Combine(data.DataFolder, "settings.json")));
            Assert.True(File.Exists(Path.Combine(data.DataFolder, "stats.json")));
        }
        finally { AppStorage.Configure(original.IsSeparate ? ["--data-directory", original.Directory] : []); }
    }

    [Fact]
    public void Invalid_profile_never_silently_falls_back_to_the_normal_data_folder()
    {
        foreach (var args in new[] { new[] { "--data-directory", "relative" }, new[] { "--data-directory", "" },
            new[] { "--unknown", Path.GetTempPath() }, new[] { "--data-directory" },
            new[] { "--data-directory", AppStorage.Resolve([]).Directory } })
            Assert.Throws<ArgumentException>(() => AppStorage.Resolve(args));
    }

    [Fact]
    public void Canonical_profile_paths_and_startup_guard_keep_the_normal_run_entry_unchanged()
    {
        using var folder = new TestFolder();
        var profile = AppStorage.Resolve(["--data-directory", Path.Combine(folder.Path, "profile", "child", "..")]);
        Assert.Equal(Path.Combine(folder.Path, "profile"), profile.Directory);
        var service = new SettingsService(profile);
        object? before = OperatingSystem.IsWindows()
            ? Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "ActiveRest", null) : null;
        Assert.False(service.CanSetAutoStart);
        Assert.False(service.IsAutoStartEnabled());
        service.SetAutoStart(true);
        service.SetAutoStart(false);
        object? after = OperatingSystem.IsWindows()
            ? Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "ActiveRest", null) : null;
        Assert.True(Equals(before, after), "A separate profile must not read or change the normal startup registration through the service.");
    }

    [Fact]
    public void Newer_history_is_not_replaced_from_an_older_backup_or_deleted()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        data.SaveOrUpdateToday(new DailyStat { TotalFocusedSeconds = 1 });
        data.SaveOrUpdateToday(new DailyStat { TotalFocusedSeconds = 2 });
        var path = Path.Combine(folder.Path, "stats.json");
        var history = JArray.Parse(File.ReadAllText(path));
        ((JObject)history[0])["FutureVersionField"] = "preserve me";
        File.WriteAllText(path, history.ToString());
        var original = File.ReadAllBytes(path);
        var backup = File.ReadAllBytes(path + ".bak");
        Assert.Empty(data.LoadAllData());
        Assert.False(data.CanWrite);
        Assert.Throws<InvalidDataException>(() => data.DeleteAllData());
        Assert.Throws<InvalidDataException>(() => data.SaveOrUpdateToday(new DailyStat()));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal(backup, File.ReadAllBytes(path + ".bak"));
        Assert.Empty(Directory.GetFiles(folder.Path, "*.corrupt-*"));
    }

    [Theory]
    [InlineData("{partial")]
    [InlineData("{\"EyeRestIntervalMinutes\":20,\"IsEyeRestEnabled\":true,\"FutureVersionField\":1}")]
    [InlineData("{\"EyeRestIntervalMinutes\":1,\"IsEyeRestEnabled\":true}")]
    public void Damaged_or_newer_settings_are_not_silently_overwritten(string content)
    {
        using var folder = new TestFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, content);
        var data = new DataManager(folder.Path);
        Assert.Equal(20, data.LoadSettings().EyeRestIntervalMinutes);
        Assert.False(data.CanWriteSettings);
        Assert.NotNull(data.SettingsRecoveryNotice);
        Assert.Throws<InvalidDataException>(() => data.SaveSettings(new AppSettings { EyeRestIntervalMinutes = 5 }));
        Assert.Equal(content, File.ReadAllText(path));
        Assert.False(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Delete_does_not_discard_a_damaged_backup_of_valid_history()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        data.SaveOrUpdateToday(new DailyStat { TotalFocusedSeconds = 1 });
        var path = Path.Combine(folder.Path, "stats.json");
        File.WriteAllText(path + ".bak", "[partial");
        var original = File.ReadAllBytes(path);
        Assert.Throws<InvalidDataException>(() => data.DeleteAllData());
        Assert.Throws<InvalidDataException>(() => data.SaveOrUpdateToday(new DailyStat { TotalFocusedSeconds = 2 }));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal("[partial", File.ReadAllText(path + ".bak"));
    }

    [Fact]
    public void Settings_save_does_not_overwrite_a_damaged_backup()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        data.SaveSettings(new AppSettings { EyeRestIntervalMinutes = 10 });
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path + ".bak", "{partial");
        var original = File.ReadAllBytes(path);
        Assert.Throws<InvalidDataException>(() => data.SaveSettings(new AppSettings { EyeRestIntervalMinutes = 5 }));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal("{partial", File.ReadAllText(path + ".bak"));
    }

    [Fact]
    public async Task Real_timer_measures_elapsed_time_and_pause_stop_preserve_one_snapshot()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        var activity = new FixtureActivity();
        using var engine = new TrackerEngine(activity, data);
        engine.StartTracking();
        engine.StartTracking();
        await WaitUntil(() => engine.GetCurrentSnapshot().TotalFocusedSeconds >= 0.5);
        engine.Pause();
        var paused = engine.GetCurrentSnapshot().TotalFocusedSeconds;
        Assert.InRange(paused, 0.5, 5);
        Assert.Equal(paused, data.LoadAllData().Single().TotalFocusedSeconds, 6);
        await Task.Delay(1250);
        Assert.Equal(paused, engine.GetCurrentSnapshot().TotalFocusedSeconds);
        engine.Resume();
        await WaitUntil(() => engine.GetCurrentSnapshot().TotalFocusedSeconds > paused + 0.5);
        engine.StopTracking();
        var final = engine.GetCurrentSnapshot();
        Assert.False(engine.IsRunning);
        Assert.Equal(final.TotalFocusedSeconds, final.AppUsageSeconds["FixtureEditor"], 6);
        Assert.Equal(final.TotalFocusedSeconds, data.LoadAllData().Single().TotalFocusedSeconds, 6);
        final.AppUsageSeconds["FixtureEditor"] = -1;
        Assert.True(engine.GetCurrentSnapshot().AppUsageSeconds["FixtureEditor"] > 0);
        engine.StopTracking();
    }

    [Fact]
    public void Weekly_view_reloads_the_selected_start_day_and_matches_both_cultures()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        foreach (var (date, seconds) in new[] { (new DateTime(2025, 12, 29), 10), (new DateTime(2026, 1, 4), 20),
            (new DateTime(2026, 1, 5), 40), (new DateTime(2026, 1, 11), 80) })
            data.SaveOrUpdateToday(new DailyStat { Date = date, TotalFocusedSeconds = seconds });
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "tr-TR", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                data.SaveSettings(new AppSettings());
                using var vm = new MainWindowViewModel(data, new FixtureActivity());
                vm.SelectedDate = new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero);
                vm.ChangeViewModeCommand.Execute("Weekly");
                Assert.Equal("00h 00m 30s", vm.FocusedTimeText);
                Assert.Contains("29", vm.CurrentModeText);
                Assert.Contains("2025", vm.CurrentModeText);
                Assert.Contains("04", vm.CurrentModeText);
                Assert.Contains("2026", vm.CurrentModeText);
                Assert.EndsWith("· Week", vm.CurrentModeText);
                data.SaveSettings(new AppSettings { WeekStartsOnSunday = true });
                vm.ReloadSettings();
                Assert.Equal("00h 01m 00s", vm.FocusedTimeText);
                Assert.DoesNotContain("2025", vm.CurrentModeText);
                Assert.Contains("04", vm.CurrentModeText);
                Assert.Contains("10", vm.CurrentModeText);
                Assert.EndsWith("· Week", vm.CurrentModeText);
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Pdf_service_renders_the_selected_fixture_report_without_changing_history()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        data.SaveOrUpdateToday(new DailyStat
        {
            Date = new DateTime(2026, 1, 4), TotalFocusedSeconds = 20,
            AppUsageSeconds = new Dictionary<string, double> { ["FixtureEditor"] = 20 }
        });
        var history = File.ReadAllBytes(Path.Combine(folder.Path, "stats.json"));
        using var vm = new MainWindowViewModel(data, new FixtureActivity());
        vm.SelectedDate = new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("00h 00m 20s", vm.FocusedTimeText);
        Assert.Equal("FixtureEditor", vm.AppUsageList.Single().AppName);
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var output = Path.Combine(folder.Path, "fixture-report.pdf");
        new PdfExportService().ExportReport(vm, output);
        var bytes = File.ReadAllBytes(output);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes.Take(8).ToArray()));
        Assert.True(bytes.Length > 1000);
        Assert.Equal(history, File.ReadAllBytes(Path.Combine(folder.Path, "stats.json")));
    }

    private static async Task WaitUntil(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(8);
        while (!predicate() && DateTime.UtcNow < timeout) await Task.Delay(50);
        Assert.True(predicate(), "The real timer did not produce an observed sample before the test deadline.");
    }

    private sealed class FixtureActivity : IActivityTracker
    {
        public double GetIdleTimeSeconds() => 0;
        public bool IsMediaPlaying() => false;
        public string GetActiveAppName() => "FixtureEditor";
    }

    private sealed class TestFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ActiveRest-profile-tests-" + Guid.NewGuid().ToString("N"));
        public TestFolder() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
