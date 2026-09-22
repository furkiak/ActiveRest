using ActiveRest.Models;
using ActiveRest.Services;
using ActiveRest.ViewModels;
using System.Globalization;

namespace ActiveRest.Tests;

public class TrackingTests
{
    [Fact]
    public void Opening_and_exiting_does_not_start_or_write_activity_history()
    {
        using var folder = new TestFolder();
        var history = System.IO.Path.Combine(folder.Path, "stats.json");
        using (var window = new MainWindowViewModel(new DataManager(folder.Path), new FakeActivity()))
        {
            Assert.Equal("Start tracking", window.PauseButtonText);
            Assert.Contains("Tracking off", window.TrackingStatusText);
            window.ForceSave();
        }
        Assert.False(File.Exists(history));
    }

    [Fact]
    public void Deleting_history_stops_tracking_until_started_again()
    {
        using var folder = new TestFolder();
        var history = System.IO.Path.Combine(folder.Path, "stats.json");
        using var window = new MainWindowViewModel(new DataManager(folder.Path), new FakeActivity());
        window.TogglePauseCommand.Execute(null);
        Assert.Equal("Pause tracking", window.PauseButtonText);

        window.DeleteHistory();
        Assert.Equal("Start tracking", window.PauseButtonText);
        Assert.Contains("Tracking off", window.TrackingStatusText);
        window.ForceSave();
        Assert.False(File.Exists(history));

        window.TogglePauseCommand.Execute(null);
        Assert.Equal("Pause tracking", window.PauseButtonText);
    }

    [Fact]
    public void Week_start_handles_sunday_year_boundary_and_culture()
    {
        foreach (var culture in new[] { "tr-TR", "en-US" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(new DateTime(2025, 12, 29), WeekMath.StartOfWeek(new DateTime(2026, 1, 4), DayOfWeek.Monday));
            Assert.Equal(new DateTime(2026, 1, 4), WeekMath.StartOfWeek(new DateTime(2026, 1, 4), DayOfWeek.Sunday));
            Assert.Equal(new DateTime(2025, 12, 29), WeekMath.StartOfWeek(new DateTime(2025, 12, 29), DayOfWeek.Monday));
        }
    }

    [Fact]
    public void Tracks_elapsed_time_splits_midnight_and_saves_previous_day()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 23, 59, 59, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), data, clock);
        engine.StartTracking();
        engine.StartTracking();
        clock.Advance(TimeSpan.FromSeconds(2));
        engine.RecordSample();
        var days = data.LoadAllData();
        Assert.Single(days);
        Assert.Equal(1, days[0].TotalFocusedSeconds, 5);
        Assert.Equal(1, engine.CurrentDayStat.TotalFocusedSeconds, 5);
        Assert.Equal(1, days[0].AppUsageSeconds["Editor"], 5);
    }

    [Fact]
    public void Delayed_sample_counts_elapsed_but_sleep_gap_is_not_misclassified()
    {
        using var folder = new TestFolder();
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), new DataManager(folder.Path), clock);
        engine.StartTracking();
        clock.Advance(TimeSpan.FromSeconds(4));
        engine.RecordSample();
        clock.Advance(TimeSpan.FromHours(2));
        engine.RecordSample();
        clock.Advance(TimeSpan.FromSeconds(1));
        engine.RecordSample();
        Assert.Equal(5, engine.CurrentDayStat.TotalFocusedSeconds, 5);
    }

    [Fact]
    public void Pause_and_resume_do_not_charge_paused_time_and_snapshot_is_isolated()
    {
        using var folder = new TestFolder();
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), new DataManager(folder.Path), clock);
        engine.StartTracking();
        engine.Pause();
        clock.Advance(TimeSpan.FromSeconds(4));
        engine.RecordSample();
        engine.Resume();
        clock.Advance(TimeSpan.FromSeconds(2));
        engine.RecordSample();
        var snapshot = engine.CurrentDayStat;
        snapshot.AppUsageSeconds["Editor"] = 999;
        Assert.Equal(2, engine.CurrentDayStat.AppUsageSeconds["Editor"], 5);
    }

    [Fact]
    public void Updated_reminder_setting_takes_effect_without_restart()
    {
        using var folder = new TestFolder();
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), new DataManager(folder.Path), clock);
        var reminders = 0;
        engine.RestReminderDue += () => reminders++;
        engine.StartTracking();
        for (var i = 0; i < 60; i++) { clock.Advance(TimeSpan.FromSeconds(4)); engine.RecordSample(); }
        Assert.Equal(0, reminders);
        engine.ApplySettings(new AppSettings { EyeRestIntervalMinutes = 5 });
        for (var i = 0; i < 15; i++) { clock.Advance(TimeSpan.FromSeconds(4)); engine.RecordSample(); }
        Assert.Equal(1, reminders);
    }

    [Fact]
    public void Damaged_primary_restores_backup_without_overwriting_it()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        var day = new DailyStat { Date = new DateTime(2026, 9, 22), TotalFocusedSeconds = 10 };
        data.SaveOrUpdateToday(day);
        day.TotalFocusedSeconds = 20;
        data.SaveOrUpdateToday(day);
        File.WriteAllText(System.IO.Path.Combine(folder.Path, "stats.json"), "{half-written");
        var recovered = data.LoadAllData();
        Assert.Equal(10, recovered.Single().TotalFocusedSeconds);
        Assert.Contains("restored", data.RecoveryNotice);
        Assert.Single(Directory.GetFiles(folder.Path, "*.corrupt-*"));
        data.SaveOrUpdateToday(recovered.Single());
        Assert.Equal(10, data.LoadAllData().Single().TotalFocusedSeconds);
    }

    [Fact]
    public void Damaged_only_copy_blocks_writes_and_preserves_original()
    {
        using var folder = new TestFolder();
        var path = System.IO.Path.Combine(folder.Path, "stats.json");
        File.WriteAllText(path, "[partial");
        var data = new DataManager(folder.Path);
        Assert.Empty(data.LoadAllData());
        Assert.False(data.CanWrite);
        Assert.Throws<InvalidDataException>(() => data.SaveOrUpdateToday(new DailyStat()));
        Assert.Equal("[partial", File.ReadAllText(path));
    }

    [Fact]
    public void Export_and_delete_cover_main_and_backup()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        data.SaveOrUpdateToday(new DailyStat());
        data.SaveOrUpdateToday(new DailyStat());
        var export = System.IO.Path.Combine(folder.Path, "export.json");
        data.ExportAllData(export);
        Assert.Contains("TotalFocusedSeconds", File.ReadAllText(export));
        data.DeleteAllData();
        Assert.False(File.Exists(System.IO.Path.Combine(folder.Path, "stats.json")));
        Assert.False(File.Exists(System.IO.Path.Combine(folder.Path, "stats.json.bak")));
    }

    [Fact]
    public void Missing_primary_with_invalid_backup_does_not_allow_new_history()
    {
        using var folder = new TestFolder();
        var backup = System.IO.Path.Combine(folder.Path, "stats.json.bak");
        File.WriteAllText(backup, "{} incomplete");
        var data = new DataManager(folder.Path);
        Assert.Empty(data.LoadAllData());
        Assert.False(data.CanWrite);
        Assert.Throws<InvalidDataException>(() => data.SaveOrUpdateToday(new DailyStat()));
        Assert.Equal("{} incomplete", File.ReadAllText(backup));
    }

    [Fact]
    public void Long_sleep_across_midnight_saves_old_day_without_charging_gap()
    {
        using var folder = new TestFolder();
        var data = new DataManager(folder.Path);
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 23, 59, 58, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), data, clock);
        engine.StartTracking();
        clock.Advance(TimeSpan.FromSeconds(1)); engine.RecordSample();
        clock.Advance(TimeSpan.FromHours(8)); engine.RecordSample();
        Assert.Equal(1, data.LoadAllData().Single().TotalFocusedSeconds, 5);
        Assert.Equal(0, engine.CurrentDayStat.TotalFocusedSeconds);
    }

    [Fact]
    public async Task Snapshots_can_be_read_while_samples_update()
    {
        using var folder = new TestFolder();
        var clock = new FakeClock(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
        using var engine = new TrackerEngine(new FakeActivity(), new DataManager(folder.Path), clock);
        engine.StartTracking();
        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var snapshot = engine.GetCurrentSnapshot();
                _ = snapshot.AppUsageSeconds.Count;
            }
        });
        for (var i = 0; i < 100; i++) { clock.Advance(TimeSpan.FromSeconds(1)); engine.RecordSample(); }
        await reader;
        Assert.Equal(100, engine.CurrentDayStat.TotalFocusedSeconds, 5);
    }

    private sealed class FakeActivity : IActivityTracker
    {
        public double GetIdleTimeSeconds() => 0;
        public bool IsMediaPlaying() => false;
        public string GetActiveAppName() => "Editor";
    }

    private sealed class FakeClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;
        private long _timestamp;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override DateTimeOffset GetUtcNow() => _now;
        public override long GetTimestamp() => _timestamp;
        public void Advance(TimeSpan duration) { _now += duration; _timestamp += duration.Ticks; }
    }

    private sealed class TestFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ActiveRest-tests-" + Guid.NewGuid().ToString("N"));
        public TestFolder() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
