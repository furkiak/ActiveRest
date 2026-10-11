using ActiveRest.Models;
using ActiveRest.Services;
using System;
using Xunit;

namespace ActiveRest.Tests
{
    public class TrackerEngineTests
    {
        private sealed class Rig : IDisposable
        {
            public TempFolder Folder = new();
            public FakeTracker Tracker = new();
            public FakeClock Clock;
            public DataManager Data;
            public SettingsStore Settings;
            public TrackerEngine Engine;

            public Rig(DateTime? start = null)
            {
                Clock = new FakeClock(start ?? new DateTime(2026, 10, 5, 9, 0, 0));
                Data = new DataManager(Folder.Path);
                Settings = new SettingsStore(Folder.Path);
                Engine = new TrackerEngine(Tracker, Data, Settings, Clock.Read);
            }

            public void Seconds(int n)
            {
                for (int i = 0; i < n; i++) { Clock.Advance(1); Engine.Tick(); }
            }

            public void Dispose() => Folder.Dispose();
        }

        [Fact]
        public void Active_user_accumulates_focus_per_app_and_per_hour()
        {
            using var rig = new Rig();
            rig.Seconds(30);

            Assert.Equal(ActivityMode.Focus, rig.Engine.Mode);
            Assert.Equal(30, rig.Engine.Today.TotalFocusedSeconds, 3);
            Assert.Equal(30, rig.Engine.Today.AppUsageSeconds["Code"], 3);
            Assert.Equal(30, rig.Engine.Today.HourlyFocusSeconds[9], 3);
            Assert.Equal(30, rig.Engine.Today.LongestFocusSeconds, 3);
        }

        [Fact]
        public void Idle_with_sustained_audio_is_relaxed_otherwise_rest()
        {
            using var rig = new Rig();
            rig.Tracker.Idle = 600;

            rig.Seconds(10);
            Assert.Equal(ActivityMode.Rest, rig.Engine.Mode);

            rig.Tracker.Media = true;
            rig.Seconds(10);
            Assert.Equal(ActivityMode.Relaxed, rig.Engine.Mode);
            Assert.True(rig.Engine.Today.TotalRelaxedSeconds > 5);
            Assert.True(rig.Engine.Today.TotalBreakSeconds > 5);
            Assert.Equal(0, rig.Engine.Today.TotalFocusedSeconds);
        }

        [Fact]
        public void A_short_notification_sound_does_not_flip_rest_to_relaxed()
        {
            using var rig = new Rig();
            rig.Tracker.Idle = 600;
            rig.Seconds(10);

            rig.Tracker.Media = true;
            rig.Seconds(1);
            rig.Tracker.Media = false;
            rig.Seconds(10);

            Assert.Equal(ActivityMode.Rest, rig.Engine.Mode);
            Assert.True(rig.Engine.Today.TotalRelaxedSeconds <= 0.001);
        }

        [Fact]
        public void Idle_threshold_comes_from_settings()
        {
            using var rig = new Rig();
            rig.Settings.Update(s => s.IdleThresholdMinutes = 1);
            rig.Tracker.Idle = 61;
            rig.Seconds(5);

            Assert.Equal(ActivityMode.Rest, rig.Engine.Mode);

            rig.Settings.Update(s => s.IdleThresholdMinutes = 5);
            rig.Seconds(5);
            Assert.Equal(ActivityMode.Focus, rig.Engine.Mode);
        }

        [Fact]
        public void Paused_engine_counts_nothing()
        {
            using var rig = new Rig();
            rig.Engine.SetPaused(true);
            rig.Seconds(20);

            Assert.Equal(ActivityMode.Paused, rig.Engine.Mode);
            Assert.Equal(0, rig.Engine.Today.TotalSeconds);

            rig.Engine.SetPaused(false);
            rig.Seconds(5);
            Assert.Equal(5, rig.Engine.Today.TotalFocusedSeconds, 3);
        }

        [Fact]
        public void A_long_gap_such_as_sleep_is_not_attributed_to_any_mode()
        {
            using var rig = new Rig();
            rig.Seconds(10);
            rig.Clock.Advance(3 * 3600); // laptop lid closed
            rig.Engine.Tick();
            rig.Seconds(5);

            Assert.Equal(15, rig.Engine.Today.TotalFocusedSeconds, 3);
        }

        [Fact]
        public void Eye_rest_fires_after_the_configured_interval_of_continuous_focus()
        {
            using var rig = new Rig();
            rig.Settings.Update(s => s.EyeRestIntervalMinutes = 5);
            int fired = 0;
            rig.Engine.EyeRestDue += (_, _) => fired++;

            rig.Seconds(5 * 60 - 1);
            Assert.Equal(0, fired);
            rig.Seconds(2);
            Assert.Equal(1, fired);
        }

        [Fact]
        public void Eye_rest_can_be_disabled_and_snoozed()
        {
            using var rig = new Rig();
            rig.Settings.Update(s => { s.EyeRestIntervalMinutes = 5; s.IsEyeRestEnabled = false; });
            int fired = 0;
            rig.Engine.EyeRestDue += (_, _) => fired++;
            rig.Seconds(6 * 60);
            Assert.Equal(0, fired);

            rig.Settings.Update(s => s.IsEyeRestEnabled = true);
            rig.Engine.Snooze(TimeSpan.FromMinutes(5)); // counter restarts from zero
            rig.Seconds(4 * 60);
            Assert.Equal(0, fired);
            rig.Seconds(70);
            Assert.Equal(1, fired);
        }

        [Fact]
        public void A_real_break_resets_the_eye_rest_timer()
        {
            using var rig = new Rig();
            rig.Settings.Update(s => s.EyeRestIntervalMinutes = 5);
            int fired = 0;
            rig.Engine.EyeRestDue += (_, _) => fired++;

            rig.Seconds(4 * 60);
            rig.Tracker.Idle = 90; // away for over a minute, still below the idle threshold
            rig.Seconds(5);
            rig.Tracker.Idle = 0;
            rig.Seconds(4 * 60);

            Assert.Equal(0, fired);
        }

        [Fact]
        public void Longest_focus_run_resets_after_a_break()
        {
            using var rig = new Rig();
            rig.Seconds(20);
            rig.Tracker.Idle = 600;
            rig.Seconds(5);
            rig.Tracker.Idle = 0;
            rig.Seconds(8);

            Assert.Equal(28, rig.Engine.Today.TotalFocusedSeconds, 3);
            Assert.Equal(20, rig.Engine.Today.LongestFocusSeconds, 3);
        }

        [Fact]
        public void Midnight_rollover_saves_yesterday_and_starts_a_fresh_day()
        {
            using var rig = new Rig(new DateTime(2026, 10, 5, 23, 59, 50));
            rig.Seconds(8);
            var changed = 0;
            rig.Engine.DayChanged += (_, _) => changed++;

            rig.Seconds(5);

            Assert.Equal(1, changed);
            Assert.Equal(new DateTime(2026, 10, 6), rig.Engine.Today.Date);
            Assert.True(rig.Engine.Today.TotalFocusedSeconds is > 0 and < 6);
            var yesterday = rig.Data.GetDay(new DateTime(2026, 10, 5));
            Assert.NotNull(yesterday);
            Assert.Equal(9, yesterday!.TotalFocusedSeconds, 3); // ticks up to and including 23:59:59
        }

        [Fact]
        public void Today_is_restored_after_a_restart()
        {
            using var rig = new Rig();
            rig.Seconds(40);
            rig.Engine.SaveBlocking();

            var engine2 = new TrackerEngine(rig.Tracker, new DataManager(rig.Folder.Path), rig.Settings, rig.Clock.Read);

            Assert.Equal(40, engine2.Today.TotalFocusedSeconds, 3);
        }

        [Fact]
        public void Data_is_written_roughly_once_a_minute_and_on_demand()
        {
            using var rig = new Rig();
            rig.Seconds(30);
            Assert.Null(rig.Data.GetDay(rig.Clock.Now.Date));

            rig.Seconds(35);
            Assert.NotNull(rig.Data.GetDay(rig.Clock.Now.Date));
        }
    }
}
