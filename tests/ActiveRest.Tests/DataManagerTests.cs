using ActiveRest.Models;
using ActiveRest.Services;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ActiveRest.Tests
{
    public class DataManagerTests
    {
        private static DailyStat Sample(DateTime date, double focus)
        {
            var d = new DailyStat(date) { TotalFocusedSeconds = focus, LongestFocusSeconds = 42 };
            d.AppUsageSeconds["Code"] = focus;
            d.HourlyFocusSeconds[10] = focus;
            return d;
        }

        [Fact]
        public void Saved_data_survives_a_restart()
        {
            using var tmp = new TempFolder();
            var first = new DataManager(tmp.Path);
            first.Upsert(Sample(new DateTime(2026, 10, 5), 1234));
            first.Flush();

            var second = new DataManager(tmp.Path);
            var day = second.GetDay(new DateTime(2026, 10, 5));

            Assert.NotNull(day);
            Assert.Equal(1234, day!.TotalFocusedSeconds);
            Assert.Equal(1234, day.AppUsageSeconds["Code"]);
            Assert.Equal(1234, day.HourlyFocusSeconds[10]);
            Assert.Equal(42, day.LongestFocusSeconds);
        }

        [Fact]
        public void Upsert_stores_a_copy_so_later_edits_do_not_leak_in()
        {
            using var tmp = new TempFolder();
            var dm = new DataManager(tmp.Path);
            var live = Sample(new DateTime(2026, 10, 5), 10);
            dm.Upsert(live);
            live.TotalFocusedSeconds = 999;

            Assert.Equal(10, dm.GetDay(live.Date)!.TotalFocusedSeconds);
        }

        [Fact]
        public void Corrupt_file_is_quarantined_and_backup_is_restored_instead_of_losing_history()
        {
            using var tmp = new TempFolder();
            var dm = new DataManager(tmp.Path);
            dm.Upsert(Sample(new DateTime(2026, 10, 5), 100));
            dm.Flush();
            dm.Upsert(Sample(new DateTime(2026, 10, 6), 200));
            dm.Flush(); // the second write turns the first file into stats.json.bak

            File.WriteAllText(Path.Combine(tmp.Path, "stats.json"), "{ this is not json");

            var reloaded = new DataManager(tmp.Path);
            Assert.NotNull(reloaded.GetDay(new DateTime(2026, 10, 5)));
            Assert.Contains(Directory.GetFiles(tmp.Path), f => Path.GetFileName(f).StartsWith("stats.corrupt-"));
        }

        [Fact]
        public void Corrupt_file_without_backup_starts_empty_but_keeps_the_damaged_copy()
        {
            using var tmp = new TempFolder();
            File.WriteAllText(Path.Combine(tmp.Path, "stats.json"), "garbage");

            var dm = new DataManager(tmp.Path);

            Assert.Empty(dm.GetAll());
            Assert.Contains(Directory.GetFiles(tmp.Path), f => Path.GetFileName(f).StartsWith("stats.corrupt-"));
        }

        [Fact]
        public void Legacy_files_without_new_fields_load_with_valid_defaults()
        {
            using var tmp = new TempFolder();
            File.WriteAllText(Path.Combine(tmp.Path, "stats.json"), """
                [{"Date":"2026-10-05T00:00:00","FirstBootTime":"2026-10-05T08:00:00","TotalFocusedSeconds":60.0,
                  "TotalRelaxedSeconds":0.0,"TotalBreakSeconds":5.0,"AppUsageSeconds":{"Chrome":60.0}}]
                """);

            var day = new DataManager(tmp.Path).GetDay(new DateTime(2026, 10, 5));

            Assert.NotNull(day);
            Assert.Equal(DailyStat.HoursPerDay, day!.HourlyFocusSeconds.Length);
            Assert.Equal(60, day.AppUsageSeconds["Chrome"]);
        }

        [Fact]
        public void Duplicate_days_in_a_damaged_file_are_merged()
        {
            using var tmp = new TempFolder();
            File.WriteAllText(Path.Combine(tmp.Path, "stats.json"), """
                [{"Date":"2026-10-05T00:00:00","TotalFocusedSeconds":60.0,"AppUsageSeconds":{"A":60.0}},
                 {"Date":"2026-10-05T00:00:00","TotalFocusedSeconds":40.0,"AppUsageSeconds":{"A":40.0}}]
                """);

            var all = new DataManager(tmp.Path).GetAll();

            Assert.Single(all);
            Assert.Equal(100, all[0].TotalFocusedSeconds);
            Assert.Equal(100, all[0].AppUsageSeconds["A"]);
        }

        [Fact]
        public void Range_query_is_start_inclusive_end_exclusive()
        {
            using var tmp = new TempFolder();
            var dm = new DataManager(tmp.Path);
            for (int i = 0; i < 5; i++) dm.Upsert(Sample(new DateTime(2026, 10, 1).AddDays(i), i));

            var range = dm.GetRange(new DateTime(2026, 10, 2), new DateTime(2026, 10, 4));

            Assert.Equal(new[] { 2, 3 }, range.Select(d => d.Date.Day).ToArray());
        }

        [Fact]
        public async System.Threading.Tasks.Task Concurrent_upserts_and_flushes_keep_the_file_valid()
        {
            using var tmp = new TempFolder();
            var dm = new DataManager(tmp.Path);

            var tasks = Enumerable.Range(0, 8).Select(worker => System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 40; i++)
                {
                    dm.Upsert(Sample(new DateTime(2026, 10, 5), worker * 1000 + i));
                    dm.Flush();
                }
            })).ToArray();
            await System.Threading.Tasks.Task.WhenAll(tasks);

            Assert.Equal(0, dm.WriteFailures);   // overlapping writers used to collide on the temp file

            // The final state, whatever it is, must be on disk and readable.
            dm.Upsert(Sample(new DateTime(2026, 10, 6), 4242));
            dm.Flush();

            var reloaded = new DataManager(tmp.Path);
            Assert.Equal(4242, reloaded.GetDay(new DateTime(2026, 10, 6))!.TotalFocusedSeconds);
            Assert.NotNull(reloaded.GetDay(new DateTime(2026, 10, 5)));
            Assert.DoesNotContain(Directory.GetFiles(tmp.Path), f => Path.GetFileName(f).StartsWith("stats.corrupt-"));
        }

        [Fact]
        public void Settings_are_clamped_and_persisted()
        {
            using var tmp = new TempFolder();
            var store = new SettingsStore(tmp.Path);
            int notified = 0;
            store.Changed += () => notified++;

            store.Update(s => { s.EyeRestIntervalMinutes = 9999; s.IdleThresholdMinutes = -3; s.Language = "xx"; s.Theme = "dark"; });

            Assert.Equal(1, notified);
            var reloaded = new SettingsStore(tmp.Path).Current;
            Assert.Equal(AppSettings.MaxEyeInterval, reloaded.EyeRestIntervalMinutes);
            Assert.Equal(AppSettings.MinIdleMinutes, reloaded.IdleThresholdMinutes);
            Assert.Equal("auto", reloaded.Language);
            Assert.Equal("dark", reloaded.Theme);
        }

        [Fact]
        public void Auto_start_command_quotes_the_path_and_starts_minimized()
        {
            Assert.Equal("\"C:\\Program Files\\ActiveRest\\ActiveRest.exe\" --minimized",
                StartupService.BuildCommand(@"C:\Program Files\ActiveRest\ActiveRest.exe"));
        }
    }
}
