using ActiveRest.Models;
using System;
using System.Linq;
using System.Timers;

namespace ActiveRest.Services;

public sealed class TrackerEngine : IDisposable
{
    private const double MaxObservedGapSeconds = 5;
    private readonly object _gate = new();
    private readonly IActivityTracker _activityTracker;
    private readonly DataManager _dataManager;
    private readonly TimeProvider _clock;
    private Timer? _timer;
    private DailyStat _currentDay;
    private AppSettings _settings;
    private long _lastTimestamp;
    private DateTime _lastLocalTime;
    private double _focusSinceReminder;
    private double _secondsSinceSave;
    private bool _running;
    private bool _paused;

    public event Action? RestReminderDue;
    public event Action<Exception>? TrackingFailed;

    public TrackerEngine(IActivityTracker activityTracker, DataManager dataManager, TimeProvider? clock = null)
    {
        _activityTracker = activityTracker;
        _dataManager = dataManager;
        _clock = clock ?? TimeProvider.System;
        _settings = _dataManager.LoadSettings();
        _currentDay = LoadDay(_clock.GetLocalNow().DateTime.Date);
    }

    public DailyStat CurrentDayStat => GetCurrentSnapshot();
    public bool IsPaused { get { lock (_gate) return _paused; } }
    public bool IsRunning { get { lock (_gate) return _running; } }

    public DailyStat GetCurrentSnapshot()
    {
        lock (_gate) return _currentDay.Copy();
    }

    public void StartTracking()
    {
        lock (_gate)
        {
            if (_running || !_dataManager.CanWrite) return;
            _running = true;
            _paused = false;
            ResetClock();
            _timer = new Timer(1000) { AutoReset = false };
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }
    }

    public void StopTracking()
    {
        lock (_gate)
        {
            if (!_running) return;
            _running = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _dataManager.SaveOrUpdateToday(_currentDay);
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (!_running || _paused) return;
            _paused = true;
            _focusSinceReminder = 0;
            _dataManager.SaveOrUpdateToday(_currentDay);
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (!_running || !_paused) return;
            _paused = false;
            ResetClock();
        }
    }

    public void ApplySettings(AppSettings settings)
    {
        if (settings.EyeRestIntervalMinutes is < 5 or > 120)
            throw new ArgumentOutOfRangeException(nameof(settings));
        lock (_gate)
        {
            _settings = new AppSettings
            {
                EyeRestIntervalMinutes = settings.EyeRestIntervalMinutes,
                IsEyeRestEnabled = settings.IsEyeRestEnabled
            };
            if (!_settings.IsEyeRestEnabled) _focusSinceReminder = 0;
        }
    }

    public void SaveSnapshot()
    {
        lock (_gate) _dataManager.SaveOrUpdateToday(_currentDay);
    }

    public void DeleteHistory()
    {
        lock (_gate)
        {
            _dataManager.DeleteAllData();
            _currentDay = new DailyStat();
            _focusSinceReminder = 0;
            _secondsSinceSave = 0;
            ResetClock();
        }
    }

    // Public for deterministic tests and explicit sampling; the timer uses the same path.
    public void RecordSample()
    {
        bool remind = false;
        lock (_gate)
        {
            if (!_running) return;
            var now = _clock.GetLocalNow().DateTime;
            var timestamp = _clock.GetTimestamp();
            var elapsed = _clock.GetElapsedTime(_lastTimestamp, timestamp).TotalSeconds;
            var previous = _lastLocalTime;
            _lastTimestamp = timestamp;
            _lastLocalTime = now;

            if (_paused || !_dataManager.CanWrite) return;
            if (elapsed <= 0 || elapsed > MaxObservedGapSeconds ||
                Math.Abs((now - previous).TotalSeconds - elapsed) > 2)
            {
                if (now.Date != _currentDay.Date.Date) RotateDay(now.Date);
                return;
            }

            var idle = _activityTracker.GetIdleTimeSeconds();
            var category = idle < 180 ? 0 : _activityTracker.IsMediaPlaying() ? 1 : 2;
            var appName = category == 0 ? _activityTracker.GetActiveAppName() : null;
            if (string.IsNullOrWhiteSpace(appName)) appName = "Unknown";

            if (now.Date != _currentDay.Date.Date)
            {
                if (previous.Date == _currentDay.Date.Date && now.Date == previous.Date.AddDays(1))
                {
                    var beforeMidnight = Math.Clamp((previous.Date.AddDays(1) - previous).TotalSeconds, 0, elapsed);
                    remind |= AddDuration(category, appName, beforeMidnight);
                    RotateDay(now.Date);
                    remind |= AddDuration(category, appName, elapsed - beforeMidnight);
                }
                else
                {
                    RotateDay(now.Date);
                }
            }
            else
            {
                remind = AddDuration(category, appName, elapsed);
            }

            if (_secondsSinceSave >= 60)
            {
                _dataManager.SaveOrUpdateToday(_currentDay);
                _secondsSinceSave = 0;
            }
        }
        if (remind) RestReminderDue?.Invoke();
    }

    private bool AddDuration(int category, string appName, double seconds)
    {
        if (seconds <= 0) return false;
        _secondsSinceSave += seconds;
        if (category == 0)
        {
            _currentDay.TotalFocusedSeconds += seconds;
            _currentDay.AppUsageSeconds.TryGetValue(appName, out var usage);
            _currentDay.AppUsageSeconds[appName] = usage + seconds;
            _focusSinceReminder += seconds;
            if (_settings.IsEyeRestEnabled && _focusSinceReminder >= _settings.EyeRestIntervalMinutes * 60)
            {
                _focusSinceReminder %= _settings.EyeRestIntervalMinutes * 60;
                return true;
            }
        }
        else
        {
            if (category == 1) _currentDay.TotalRelaxedSeconds += seconds;
            else _currentDay.TotalBreakSeconds += seconds;
            _focusSinceReminder = 0;
        }
        return false;
    }

    private void RotateDay(DateTime date)
    {
        _dataManager.SaveOrUpdateToday(_currentDay);
        _currentDay = LoadDay(date);
        _secondsSinceSave = 0;
    }

    private DailyStat LoadDay(DateTime date) =>
        _dataManager.LoadAllData().FirstOrDefault(d => d.Date.Date == date) ??
        new DailyStat { Date = date, FirstBootTime = _clock.GetLocalNow().DateTime };

    private void ResetClock()
    {
        _lastTimestamp = _clock.GetTimestamp();
        _lastLocalTime = _clock.GetLocalNow().DateTime;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try { RecordSample(); }
        catch (Exception exception)
        {
            lock (_gate) _paused = true;
            TrackingFailed?.Invoke(exception);
        }
        finally
        {
            lock (_gate) { if (_running) _timer?.Start(); }
        }
    }

    public void Dispose() => StopTracking();
}
