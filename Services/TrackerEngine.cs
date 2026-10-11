using ActiveRest.Models;
using System;
using System.Collections.Generic;

namespace ActiveRest.Services
{
    public sealed class EyeRestDueEventArgs : EventArgs
    {
        public int BreakSeconds { get; init; } = 20;
        public int SnoozeMinutes { get; init; } = 5;
    }

    /// <summary>
    /// Classifies each second as focus, relaxed or rest and accumulates it into today's record.
    /// The engine owns no timer and touches no UI: the host calls <see cref="Tick"/> about once a second
    /// from a single thread, which keeps all state free of locking and makes the logic unit-testable.
    /// </summary>
    public sealed class TrackerEngine
    {
        /// <summary>A gap longer than this means the machine slept or the clock jumped; the gap is not attributed to any mode.</summary>
        public const double MaxTickGapSeconds = 10;

        /// <summary>Inactivity that already counts as a real break for the eye-rest timer.</summary>
        public const double EyeBreakResetIdleSeconds = 60;

        private const int AudioWindowSize = 5;
        private const int AudioSamplesRequired = 3;
        private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(60);

        private readonly IActivityTracker _tracker;
        private readonly DataManager _data;
        private readonly SettingsStore _settings;
        private readonly Func<DateTime> _clock;
        private readonly bool[] _audioWindow = new bool[AudioWindowSize];
        private int _audioIndex;
        private DateTime _lastTick;
        private DateTime _lastSave;
        private double _continuousFocusSeconds;
        private double _focusRunSeconds;

        public DailyStat Today { get; private set; }
        public ActivityMode Mode { get; private set; } = ActivityMode.Rest;
        public bool IsPaused { get; private set; }
        public string CurrentApp { get; private set; } = string.Empty;

        /// <summary>Raised when the eye-rest interval has been worked through. Fires on the thread that calls <see cref="Tick"/>.</summary>
        public event EventHandler<EyeRestDueEventArgs>? EyeRestDue;

        /// <summary>Raised when the calendar day rolled over and <see cref="Today"/> is a new record.</summary>
        public event EventHandler? DayChanged;

        public TrackerEngine(IActivityTracker tracker, DataManager data, SettingsStore settings, Func<DateTime>? clock = null)
        {
            _tracker = tracker;
            _data = data;
            _settings = settings;
            _clock = clock ?? (() => DateTime.Now);
            _lastTick = _lastSave = _clock();
            Today = LoadDay(_lastTick.Date);
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            _lastTick = _clock();
            if (paused)
            {
                Mode = ActivityMode.Paused;
                ResetRuns();
                Save();
            }
        }

        /// <summary>Pushes the next eye-rest reminder back by the given amount.</summary>
        public void Snooze(TimeSpan delay)
        {
            double interval = _settings.Current.EyeRestIntervalMinutes * 60.0;
            _continuousFocusSeconds = Math.Max(0, interval - delay.TotalSeconds);
        }

        public void Tick()
        {
            DateTime now = _clock();
            double elapsed = (now - _lastTick).TotalSeconds;
            _lastTick = now;
            if (elapsed <= 0) return;

            if (now.Date != Today.Date) RollOver(now);

            if (IsPaused)
            {
                Mode = ActivityMode.Paused;
                return;
            }

            if (elapsed > MaxTickGapSeconds)
            {
                // Suspend/resume or a clock change: nothing is known about the gap.
                ResetRuns();
                return;
            }

            Sample(now, elapsed);

            if (now - _lastSave >= SaveInterval) Save();
        }

        /// <summary>Persists today's record immediately (used on exit, pause and day change).</summary>
        public void Save()
        {
            _lastSave = _clock();
            _data.Upsert(Today);
            _ = _data.FlushAsync();
        }

        /// <summary>Synchronous variant of <see cref="Save"/> for process shutdown.</summary>
        public void SaveBlocking()
        {
            _lastSave = _clock();
            _data.Upsert(Today);
            _data.Flush();
        }

        private void Sample(DateTime now, double elapsed)
        {
            var settings = _settings.Current;
            double idle = _tracker.GetIdleTimeSeconds();
            bool media = RecordAudioSample(_tracker.IsMediaPlaying());
            double idleLimit = settings.IdleThresholdMinutes * 60.0;

            if (idle < idleLimit)
            {
                Mode = ActivityMode.Focus;
                Today.TotalFocusedSeconds += elapsed;
                Today.HourlyFocusSeconds[now.Hour] += elapsed;

                string app = _tracker.GetActiveAppName();
                CurrentApp = app;
                Today.AppUsageSeconds[app] = Today.AppUsageSeconds.GetValueOrDefault(app) + elapsed;

                _focusRunSeconds += elapsed;
                if (_focusRunSeconds > Today.LongestFocusSeconds) Today.LongestFocusSeconds = _focusRunSeconds;

                if (idle >= EyeBreakResetIdleSeconds) _continuousFocusSeconds = 0;
                else _continuousFocusSeconds += elapsed;

                CheckEyeRest(settings);
            }
            else
            {
                if (media)
                {
                    Mode = ActivityMode.Relaxed;
                    Today.TotalRelaxedSeconds += elapsed;
                }
                else
                {
                    Mode = ActivityMode.Rest;
                    Today.TotalBreakSeconds += elapsed;
                }
                CurrentApp = string.Empty;
                ResetRuns();
            }
        }

        private void CheckEyeRest(AppSettings settings)
        {
            if (!settings.IsEyeRestEnabled) return;
            if (_continuousFocusSeconds < settings.EyeRestIntervalMinutes * 60.0) return;

            _continuousFocusSeconds = 0;
            EyeRestDue?.Invoke(this, new EyeRestDueEventArgs());
        }

        /// <summary>Short notification sounds must not flip the mode, so audio counts only when it persists.</summary>
        private bool RecordAudioSample(bool active)
        {
            _audioWindow[_audioIndex] = active;
            _audioIndex = (_audioIndex + 1) % AudioWindowSize;

            int hits = 0;
            foreach (bool b in _audioWindow) if (b) hits++;
            return hits >= AudioSamplesRequired;
        }

        private void ResetRuns()
        {
            _continuousFocusSeconds = 0;
            _focusRunSeconds = 0;
        }

        private void RollOver(DateTime now)
        {
            SaveBlocking();
            Today = LoadDay(now.Date);
            ResetRuns();
            DayChanged?.Invoke(this, EventArgs.Empty);
        }

        private DailyStat LoadDay(DateTime date) => _data.GetDay(date)?.Clone() ?? new DailyStat(date);
    }
}
