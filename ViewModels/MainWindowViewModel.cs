using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActiveRest.Services;
using ActiveRest.Models;

namespace ActiveRest.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly TrackerEngine _engine;
        private readonly DataManager _dataManager;
        private readonly DispatcherTimer _uiTimer;

        [ObservableProperty] private string _currentModeText = "Today";

        [ObservableProperty] private string _dailyButtonColor = "#FFFFFF";
        [ObservableProperty] private string _weeklyButtonColor = "Transparent";
        [ObservableProperty] private string _monthlyButtonColor = "Transparent";
        [ObservableProperty] private string _yearlyButtonColor = "Transparent";

        [ObservableProperty] private string _focusedTimeText = "00h 00m 00s";
        [ObservableProperty] private string _relaxedTimeText = "00h 00m 00s";
        [ObservableProperty] private string _breakTimeText = "00h 00m 00s";

        [ObservableProperty] private List<AppUsageItem> _appUsageList = new();

        [ObservableProperty] private string _chartFocusWidth = "1*";
        [ObservableProperty] private string _chartRelaxWidth = "1*";
        [ObservableProperty] private string _chartBreakWidth = "1*";

        [ObservableProperty] private string _totalUptimeText = "00h 00m 00s";
        [ObservableProperty] private string _focusStatText = "%0 (00h 00m)";
        [ObservableProperty] private string _relaxStatText = "%0 (00h 00m)";
        [ObservableProperty] private string _breakStatText = "%0 (00h 00m)";

        [ObservableProperty] private DateTimeOffset? _selectedDate = DateTimeOffset.Now;
        [ObservableProperty] private string _trackingStatusText = "Tracking locally";
        [ObservableProperty] private string _pauseButtonText = "Pause tracking";
        [ObservableProperty] private string _dataStatusText = "";
        private string _currentMode = "Daily";
        private bool _followToday = true;

        public MainWindowViewModel()
        {
            var tracker = new WindowsActivityTracker();
            _dataManager = new DataManager();
            _engine = new TrackerEngine(tracker, _dataManager);
            _engine.RestReminderDue += () => Dispatcher.UIThread.Post(() => new Views.NotificationWindow().Show());
            _engine.TrackingFailed += ex => Dispatcher.UIThread.Post(() =>
            {
                TrackingStatusText = "Tracking paused after an error";
                DataStatusText = $"Could not save local activity: {ex.Message}";
                PauseButtonText = "Resume tracking";
            });
            _engine.StartTracking();
            if (!_dataManager.CanWrite)
            {
                TrackingStatusText = "Tracking paused · history needs recovery";
                DataStatusText = _dataManager.RecoveryNotice ?? "History needs recovery.";
            }
            else if (_dataManager.RecoveryNotice != null) DataStatusText = _dataManager.RecoveryNotice;

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiTimer.Tick += UpdateUI;
            _uiTimer.Start();
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += (s, e) => Dispose();
            }
        }

        partial void OnSelectedDateChanged(DateTimeOffset? value)
        {
            _followToday = value.HasValue && value.Value.Date == DateTime.Today;
            if (value.HasValue && value.Value.Date == DateTime.Today)
                CurrentModeText = _currentMode == "Daily" ? "Today" : CurrentModeText;
            else
                CurrentModeText = $"{value?.ToString("dd MMM yyyy")} · {_currentMode}";

            CalculateStats();
        }

        [RelayCommand]
        private void ChangeViewMode(string mode)
        {
            _currentMode = mode;
            DailyButtonColor = mode == "Daily" ? "#FFFFFF" : "Transparent";
            WeeklyButtonColor = mode == "Weekly" ? "#FFFFFF" : "Transparent";
            MonthlyButtonColor = mode == "Monthly" ? "#FFFFFF" : "Transparent";
            YearlyButtonColor = mode == "Yearly" ? "#FFFFFF" : "Transparent";

            if (mode == "Daily") CurrentModeText = "Statistics";
            else if (mode == "Weekly") CurrentModeText = "Weekly Total Statistics";
            else if (mode == "Monthly") CurrentModeText = "Monthly Total Statistics";
            else if (mode == "Yearly") CurrentModeText = "Yearly Total Statistics";

            if (mode == "Daily") SelectedDate = DateTimeOffset.Now;
            CalculateStats();
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            if (_followToday && SelectedDate?.Date != DateTime.Today)
            {
                SelectedDate = DateTimeOffset.Now;
            }
            if (_currentMode == "Daily" && SelectedDate.HasValue && SelectedDate.Value.Date == DateTime.Today)
            {
                var today = _engine.GetCurrentSnapshot();
                FocusedTimeText = FormatTime(today.TotalFocusedSeconds);
                RelaxedTimeText = FormatTime(today.TotalRelaxedSeconds);
                BreakTimeText = FormatTime(today.TotalBreakSeconds);
                UpdateDetailsData(new List<DailyStat> { today });
            }
        }

        private void CalculateStats()
        {
            try
            {
                if (_currentMode == "Daily" && SelectedDate.HasValue && SelectedDate.Value.Date == DateTime.Today)
                {
                    UpdateUI(null, EventArgs.Empty);
                    return;
                }

                var allData = _dataManager.LoadAllData();
                var liveToday = _engine.GetCurrentSnapshot();
                allData.RemoveAll(d => d.Date.Date == liveToday.Date.Date);
                allData.Add(liveToday);
                DateTime targetDate = SelectedDate?.Date ?? DateTime.Today;
                List<DailyStat> filteredData = new List<DailyStat>();

                switch (_currentMode)
                {
                    case "Daily": filteredData = allData.Where(d => d.Date.Date == targetDate).ToList(); break;
                    case "Weekly":
                        var settings = _dataManager.LoadSettings();
                        DateTime startOfWeek = WeekMath.StartOfWeek(targetDate,
                            settings.WeekStartsOnSunday ? DayOfWeek.Sunday : DayOfWeek.Monday);
                        filteredData = allData.Where(d => d.Date.Date >= startOfWeek && d.Date.Date < startOfWeek.AddDays(7)).ToList();
                        break;
                    case "Monthly": filteredData = allData.Where(d => d.Date.Year == targetDate.Year && d.Date.Month == targetDate.Month).ToList(); break;
                    case "Yearly": filteredData = allData.Where(d => d.Date.Year == targetDate.Year).ToList(); break;
                }

                double totalFocused = filteredData.Sum(d => d.TotalFocusedSeconds);
                double totalRelaxed = filteredData.Sum(d => d.TotalRelaxedSeconds);
                double totalBreak = filteredData.Sum(d => d.TotalBreakSeconds);

                FocusedTimeText = FormatTime(totalFocused);
                RelaxedTimeText = FormatTime(totalRelaxed);
                BreakTimeText = FormatTime(totalBreak);
                UpdateDetailsData(filteredData);
            }
            catch (Exception ex) { Console.WriteLine($"Error-8: {ex.Message}"); }
        }
        public void ForceSave()
        {
            if (_dataManager.CanWrite) _engine.SaveSnapshot();
        }
        public void ReloadSettings()
        {
            _engine.ApplySettings(_dataManager.LoadSettings());
            CalculateStats();
        }

        [RelayCommand]
        private void TogglePause()
        {
            if (!_engine.IsRunning) return;
            if (_engine.IsPaused)
            {
                _engine.Resume();
                TrackingStatusText = "Tracking locally";
                PauseButtonText = "Pause tracking";
            }
            else
            {
                _engine.Pause();
                TrackingStatusText = "Tracking paused";
                PauseButtonText = "Resume tracking";
            }
        }

        public void ExportHistory(string path)
        {
            if (_dataManager.CanWrite) _engine.SaveSnapshot();
            _dataManager.ExportAllData(path);
        }
        public void DeleteHistory()
        {
            _engine.DeleteHistory();
            if (!_engine.IsRunning) _engine.StartTracking();
            DataStatusText = "Local history deleted.";
            CalculateStats();
        }
        public string DataFolder => _dataManager.DataFolder;

        public void Dispose()
        {
            _uiTimer.Stop();
            if (_engine.IsRunning && _dataManager.CanWrite) _engine.StopTracking();
            else if (_engine.IsRunning) _engine.Dispose();
        }
        private void UpdateDetailsData(List<DailyStat> stats)
        {
            var appUsages = new Dictionary<string, double>();
            foreach (var stat in stats)
            {
                if (stat.AppUsageSeconds != null)
                {
                    foreach (var kvp in stat.AppUsageSeconds)
                    {
                        if (appUsages.ContainsKey(kvp.Key)) appUsages[kvp.Key] += kvp.Value;
                        else appUsages.Add(kvp.Key, kvp.Value);
                    }
                }
            }
             
            double totalAppSeconds = appUsages.Values.Sum();  

            AppUsageList = appUsages.Select(x =>
            { 
                double pct = totalAppSeconds > 0 ? (x.Value / totalAppSeconds) * 100 : 0;

                return new AppUsageItem
                {
                    AppName = x.Key,
                    TotalSeconds = x.Value,
                    DurationText = FormatTime(x.Value),
                    PercentageText = $"%{Math.Round(pct)}"  
                };
            }).OrderByDescending(x => x.TotalSeconds).ToList(); 
             

            double f = stats.Sum(x => x.TotalFocusedSeconds);
            double r = stats.Sum(x => x.TotalRelaxedSeconds);
            double b = stats.Sum(x => x.TotalBreakSeconds);
            double total = f + r + b;

            if (total > 0)
            {
                int fw = (int)Math.Round((f / total) * 100);
                int rw = (int)Math.Round((r / total) * 100);
                int bw = (int)Math.Round((b / total) * 100);

                ChartFocusWidth = $"{fw}*"; ChartRelaxWidth = $"{rw}*"; ChartBreakWidth = $"{bw}*";
                 
                FocusStatText = $"%{fw} ({FormatTime(f)})";
                RelaxStatText = $"%{rw} ({FormatTime(r)})";
                BreakStatText = $"%{bw} ({FormatTime(b)})";
            }
            else
            {
                ChartFocusWidth = "1*"; ChartRelaxWidth = "1*"; ChartBreakWidth = "1*";
                FocusStatText = "%0 (00h 00m 00s)"; RelaxStatText = "%0 (00h 00m 00s)"; BreakStatText = "%0 (00h 00m 00s)";
            }

            TotalUptimeText = FormatTime(total);
        }

        private string FormatTime(double totalSeconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}g {time.Hours:D2}s {time.Minutes:D2}d";
            return $"{time.Hours:D2}h {time.Minutes:D2}m {time.Seconds:D2}s";
        }
    }
}
