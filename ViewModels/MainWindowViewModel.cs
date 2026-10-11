using ActiveRest.Localization;
using ActiveRest.Models;
using ActiveRest.Services;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ActiveRest.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const double PlotHeight = 84;
        private const double MinChartScaleSeconds = 900;
        private const int MaxListedApps = 100;

        private readonly TrackerEngine _engine;
        private readonly DataManager _data;
        private readonly SettingsStore _settings;
        private readonly Loc _loc = Loc.Instance;

        private DateRange _range;
        private IReadOnlyList<DailyStat> _rangeDays = Array.Empty<DailyStat>();   // stored days of the range, today excluded
        private int _streakEndingYesterday;
        private bool _followToday = true;
        private PeriodSummary _summary = PeriodSummary.Empty;
        private IReadOnlyList<DailyStat> _lastDays = Array.Empty<DailyStat>();

        // ----- navigation -----
        [ObservableProperty] private PeriodKind _period = PeriodKind.Day;
        [ObservableProperty] private DateTime _anchor = DateTime.Today;
        [ObservableProperty] private DateTime? _selectedDate = DateTime.Today;
        [ObservableProperty] private string _rangeTitle = string.Empty;
        [ObservableProperty] private string _rangeSubtitle = string.Empty;
        [ObservableProperty] private string _reportTitle = string.Empty;
        [ObservableProperty] private bool _canGoNext;
        [ObservableProperty] private bool _isCurrentPeriod = true;
        [ObservableProperty] private DateTime _lastSelectableDate = DateTime.Today;

        // ----- live state -----
        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private bool _isStatusFocus;
        [ObservableProperty] private bool _isStatusRelaxed;
        [ObservableProperty] private bool _isStatusRest;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private bool _isPinned;
        [ObservableProperty] private string _trayTooltip = "ActiveRest";

        // ----- hero -----
        [ObservableProperty] private string _heroTimeText = "0m";
        [ObservableProperty] private string _goalText = string.Empty;
        [ObservableProperty] private double _ringSweep;
        [ObservableProperty] private bool _hasProgress;
        [ObservableProperty] private string _focusRatioText = "0%";
        [ObservableProperty] private string _trackedText = "0m";
        [ObservableProperty] private string _streakText = string.Empty;

        // ----- mode tiles -----
        [ObservableProperty] private string _focusTimeText = "0m";
        [ObservableProperty] private string _relaxedTimeText = "0m";
        [ObservableProperty] private string _restTimeText = "0m";
        [ObservableProperty] private string _focusPercentText = "0%";
        [ObservableProperty] private string _relaxedPercentText = "0%";
        [ObservableProperty] private string _restPercentText = "0%";
        [ObservableProperty] private GridLength _focusColumn = new(0, GridUnitType.Star);
        [ObservableProperty] private GridLength _relaxedColumn = new(0, GridUnitType.Star);
        [ObservableProperty] private GridLength _restColumn = new(0, GridUnitType.Star);

        // ----- chart -----
        [ObservableProperty] private string _chartTitle = string.Empty;
        [ObservableProperty] private string _chartCaption = string.Empty;
        [ObservableProperty] private bool _hasChartData;

        // ----- highlights (details window) -----
        [ObservableProperty] private string _peakHourText = "-";
        [ObservableProperty] private string _longestFocusText = "-";
        [ObservableProperty] private string _topAppText = "-";
        [ObservableProperty] private string _fourthInsightLabel = string.Empty;
        [ObservableProperty] private string _fourthInsightText = "-";

        public ObservableCollection<AppUsageItem> AppUsageList { get; } = new();
        public ObservableCollection<ChartBarViewModel> ChartBars { get; } = new();
        public bool HasApps => AppUsageList.Count > 0;

        /// <summary>Set by the window; while hidden the view model skips all per-second work.</summary>
        public bool IsWindowVisible { get; set; }

        public int PeriodIndex
        {
            get => (int)Period;
            set { if (value >= 0 && value != (int)Period) Period = (PeriodKind)value; }
        }

        public PeriodSummary Summary => _summary;
        public ActivityMode CurrentMode => _engine.Mode;

        public MainWindowViewModel(TrackerEngine engine, DataManager data, SettingsStore settings)
        {
            _engine = engine;
            _data = data;
            _settings = settings;

            _loc.LanguageChanged += OnLanguageChanged;
            _settings.Changed += OnSettingsChanged;
            _engine.DayChanged += OnDayChanged;

            ReloadRange();
            UpdateStatus();
        }

        // ================= commands =================

        [RelayCommand]
        private void Previous() => MoveAnchor(-1);

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void Next() => MoveAnchor(+1);

        [RelayCommand]
        private void GoToToday() => Anchor = DateTime.Today;

        [RelayCommand]
        private void TogglePause()
        {
            IsPaused = !IsPaused;
            _engine.SetPaused(IsPaused);
            UpdateStatus();
            RefreshLive();
        }

        [RelayCommand]
        private void TogglePin() => IsPinned = !IsPinned;

        /// <summary>Lets the tray menu drive the same toggle.</summary>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            TogglePause();
        }

        /// <summary>Recomputes everything immediately (used when the window is shown after being hidden).</summary>
        public void RefreshNow()
        {
            UpdateStatus();
            RefreshLive();
        }

        // ================= tick =================

        /// <summary>Called by the host about once a second, after the engine ticked.</summary>
        public void OnTick()
        {
            UpdateStatus();
            if (IsWindowVisible && _range.Contains(DateTime.Today))
                RefreshLive();
            else
                UpdateTrayTooltip();
        }

        // ================= property hooks =================

        partial void OnPeriodChanged(PeriodKind value)
        {
            OnPropertyChanged(nameof(PeriodIndex));
            ReloadRange();
        }

        partial void OnAnchorChanged(DateTime value)
        {
            _followToday = value.Date == DateTime.Today;
            SelectedDate = value.Date;
            ReloadRange();
        }

        partial void OnSelectedDateChanged(DateTime? value)
        {
            if (value is { } d && d.Date != Anchor.Date)
                Anchor = d.Date > DateTime.Today ? DateTime.Today : d.Date;
        }

        partial void OnCanGoNextChanged(bool value) => NextCommand.NotifyCanExecuteChanged();

        // ================= loading =================

        private DayOfWeek FirstDayOfWeek => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        private double GoalSeconds => _settings.Current.DailyFocusGoalMinutes * 60.0;

        private void MoveAnchor(int steps)
        {
            var next = StatsAggregator.Shift(Period, Anchor, steps);
            Anchor = next.Date > DateTime.Today ? DateTime.Today : next.Date;
        }

        /// <summary>Re-reads the stored days for the selected period (cheap: served from the in-memory cache).</summary>
        private void ReloadRange()
        {
            DateTime today = DateTime.Today;
            _range = StatsAggregator.GetRange(Period, Anchor, FirstDayOfWeek);
            _rangeDays = _data.GetRange(_range.Start, _range.EndExclusive).Where(d => d.Date != today).ToList();

            var history = _data.GetAll().Where(d => d.Date != today).ToList();
            _streakEndingYesterday = StatsAggregator.RunEndingAt(history, today.AddDays(-1), GoalSeconds);

            UpdateNavigation(today);
            RefreshLive();
        }

        private void UpdateNavigation(DateTime today)
        {
            IsCurrentPeriod = _range.Contains(today);
            var nextRange = StatsAggregator.GetRange(Period, StatsAggregator.Shift(Period, Anchor, +1), FirstDayOfWeek);
            CanGoNext = nextRange.Start <= today;
            LastSelectableDate = today;
            UpdateRangeText(today);
            ChartTitle = _loc.Get("chart." + Period.ToString().ToLowerInvariant());
        }

        private void UpdateRangeText(DateTime today)
        {
            var c = _loc.Culture;
            switch (Period)
            {
                case PeriodKind.Day:
                    RangeTitle = Anchor.Date == today ? _loc.Get("nav.today")
                        : Anchor.Date == today.AddDays(-1) ? _loc.Get("nav.yesterday")
                        : Anchor.ToString("dddd", c);
                    RangeSubtitle = Anchor.ToString("d MMMM yyyy", c);
                    break;

                case PeriodKind.Week:
                    DateTime thisWeek = StatsAggregator.StartOfWeek(today, FirstDayOfWeek);
                    RangeTitle = _range.Start == thisWeek ? _loc.Get("nav.thisWeek")
                        : _range.Start == thisWeek.AddDays(-7) ? _loc.Get("nav.lastWeek")
                        : FormatWeek(c);
                    RangeSubtitle = _range.Start == thisWeek || _range.Start == thisWeek.AddDays(-7) ? FormatWeek(c) : string.Empty;
                    break;

                case PeriodKind.Month:
                    bool thisMonth = Anchor.Year == today.Year && Anchor.Month == today.Month;
                    RangeTitle = thisMonth ? _loc.Get("nav.thisMonth") : Anchor.ToString("MMMM", c);
                    RangeSubtitle = thisMonth ? Anchor.ToString("MMMM yyyy", c) : Anchor.ToString("yyyy", c);
                    break;

                default:
                    RangeTitle = Anchor.Year == today.Year ? _loc.Get("nav.thisYear") : Anchor.ToString("yyyy", c);
                    RangeSubtitle = Anchor.Year == today.Year ? Anchor.ToString("yyyy", c) : string.Empty;
                    break;
            }

            // Also the title of exported reports, e.g. "This week · 5 – 11 Oct 2026".
            ReportTitle = string.IsNullOrEmpty(RangeSubtitle) ? RangeTitle : $"{RangeTitle} · {RangeSubtitle}";
        }

        private string FormatWeek(CultureInfo c)
        {
            DateTime s = _range.Start, e = _range.EndInclusive;
            if (s.Year != e.Year) return $"{s.ToString("d MMM yyyy", c)} – {e.ToString("d MMM yyyy", c)}";
            if (s.Month != e.Month) return $"{s.ToString("d MMM", c)} – {e.ToString("d MMM yyyy", c)}";
            return $"{s.Day} – {e.ToString("d MMM yyyy", c)}";
        }

        // ================= live refresh =================

        private IReadOnlyList<DailyStat> DaysWithLiveToday()
        {
            if (!_range.Contains(DateTime.Today)) return _rangeDays;
            var list = new List<DailyStat>(_rangeDays.Count + 1);
            list.AddRange(_rangeDays);
            list.Add(_engine.Today);
            return list;
        }

        private void RefreshLive()
        {
            DateTime now = DateTime.Now;
            double goal = GoalSeconds;
            var days = DaysWithLiveToday();
            _lastDays = days;
            _summary = StatsAggregator.Summarize(days, _range, now, goal);

            ApplySummary(_summary, goal);
            ApplyStreak(goal);
            SyncApps(_summary);
            SyncChart(days, now, goal);
            UpdateTrayTooltip();
        }

        private void ApplySummary(PeriodSummary s, double goal)
        {
            HeroTimeText = _loc.FormatDuration(s.FocusSeconds);
            FocusTimeText = _loc.FormatDuration(s.FocusSeconds);
            RelaxedTimeText = _loc.FormatDuration(s.RelaxedSeconds);
            RestTimeText = _loc.FormatDuration(s.RestSeconds);
            FocusPercentText = _loc.FormatPercent(s.FocusRatio);
            RelaxedPercentText = _loc.FormatPercent(s.RelaxedRatio);
            RestPercentText = _loc.FormatPercent(s.RestRatio);
            FocusRatioText = _loc.FormatPercent(s.FocusRatio);
            TrackedText = _loc.FormatDuration(s.TotalSeconds);

            FocusColumn = new GridLength(s.FocusRatio, GridUnitType.Star);
            RelaxedColumn = new GridLength(s.RelaxedRatio, GridUnitType.Star);
            RestColumn = new GridLength(s.RestRatio, GridUnitType.Star);

            double progress = s.GoalProgress;
            HasProgress = progress > 0.002;
            RingSweep = Math.Min(359.9, progress * 360);
            GoalText = s.GoalSeconds <= 0 ? string.Empty
                : progress >= 1 ? _loc.Get("goal.reached")
                : _loc.Format("goal.of", _loc.FormatGoal(s.GoalSeconds));

            PeakHourText = s.PeakHour is { } h ? $"{h:00}:00 – {(h + 1) % 24:00}:00" : _loc.Get("insight.none");
            LongestFocusText = s.LongestFocusSeconds > 0 ? _loc.FormatDuration(s.LongestFocusSeconds) : _loc.Get("insight.none");
            TopAppText = s.Apps.Count > 0 ? AppNameFormatter.Pretty(s.Apps[0].App) : _loc.Get("insight.none");

            if (Period == PeriodKind.Day)
            {
                FourthInsightLabel = _loc.Get("insight.tracked");
                FourthInsightText = TrackedText;
            }
            else
            {
                FourthInsightLabel = _loc.Get("insight.average");
                FourthInsightText = s.ActiveDays > 0 ? _loc.FormatDuration(s.AverageFocusPerActiveDay) : _loc.Get("insight.none");
            }
        }

        private void ApplyStreak(double goal)
        {
            int streak = _streakEndingYesterday + (_engine.Today.TotalFocusedSeconds >= goal ? 1 : 0);
            StreakText = streak > 0 ? _loc.Format("stat.streak.value", streak) : _loc.Get("stat.streak.none");
        }

        private void SyncApps(PeriodSummary s)
        {
            double total = s.AppSecondsTotal;
            var wanted = s.Apps.Take(MaxListedApps).ToList();

            for (int i = 0; i < wanted.Count; i++)
            {
                var w = wanted[i];
                int existing = -1;
                for (int j = i; j < AppUsageList.Count; j++)
                    if (AppUsageList[j].AppKey == w.App) { existing = j; break; }

                AppUsageItem item;
                if (existing < 0)
                {
                    item = new AppUsageItem { AppKey = w.App };
                    AppUsageList.Insert(i, item);
                }
                else
                {
                    if (existing != i) AppUsageList.Move(existing, i);
                    item = AppUsageList[i];
                }

                double fraction = total > 0 ? w.Seconds / total : 0;
                string display = AppNameFormatter.Pretty(w.App);
                item.AppName = display;
                item.Initial = AppNameFormatter.Initial(display);
                item.Hue = AppNameFormatter.Hue(w.App);
                item.TotalSeconds = w.Seconds;
                item.Fraction = fraction;
                item.DurationText = _loc.FormatDuration(w.Seconds);
                item.PercentageText = _loc.FormatPercent(fraction);
            }

            while (AppUsageList.Count > wanted.Count)
                AppUsageList.RemoveAt(AppUsageList.Count - 1);

            OnPropertyChanged(nameof(HasApps));
        }

        private void SyncChart(IReadOnlyList<DailyStat> days, DateTime now, double goal)
        {
            var bars = StatsAggregator.BuildChart(Period, _range, days, now, goal, _loc.Culture);
            double max = bars.Count > 0 ? bars.Max(b => b.Seconds) : 0;
            double scale = Math.Max(max, MinChartScaleSeconds);

            if (ChartBars.Count != bars.Count)
            {
                ChartBars.Clear();
                for (int i = 0; i < bars.Count; i++) ChartBars.Add(new ChartBarViewModel());
            }

            for (int i = 0; i < bars.Count; i++)
            {
                var b = bars[i];
                var vm = ChartBars[i];
                vm.Label = b.Label;
                vm.ShowLabel = b.ShowLabel;
                vm.IsCurrent = b.IsCurrent;
                vm.MetGoal = b.MetGoal;
                vm.IsEmpty = b.Seconds <= 0;
                vm.Height = b.Seconds <= 0 ? 4 : Math.Max(6, b.Seconds / scale * PlotHeight);
                string name = Period == PeriodKind.Day ? $"{i:00}:00" : b.Label;
                vm.Tooltip = $"{name} · {_loc.FormatDuration(b.Seconds)}";
            }

            HasChartData = max > 0;
            ChartCaption = max > 0 ? _loc.Format("chart.peak", _loc.FormatDuration(max)) : string.Empty;
        }

        // ================= status / tray =================

        private void UpdateStatus()
        {
            var mode = _engine.Mode;
            IsStatusFocus = mode == ActivityMode.Focus;
            IsStatusRelaxed = mode == ActivityMode.Relaxed;
            IsStatusRest = mode == ActivityMode.Rest;
            StatusText = mode switch
            {
                ActivityMode.Focus => _loc.Get("status.focus"),
                ActivityMode.Relaxed => _loc.Get("status.relaxed"),
                ActivityMode.Rest => _loc.Get("status.rest"),
                _ => _loc.Get("status.paused")
            };
        }

        private int _tooltipTicks = 59;

        private void UpdateTrayTooltip()
        {
            if (++_tooltipTicks < 30 && !IsWindowVisible) return;
            _tooltipTicks = 0;
            TrayTooltip = _engine.IsPaused
                ? _loc.Get("tray.tooltip.paused")
                : _loc.Format("tray.tooltip", _loc.FormatDuration(_engine.Today.TotalFocusedSeconds));
        }

        // ================= reactions =================

        private void OnLanguageChanged()
        {
            _tooltipTicks = 59;
            UpdateStatus();
            ReloadRange();
        }

        private void OnSettingsChanged()
        {
            ReloadRange();
        }

        private void OnDayChanged(object? sender, EventArgs e)
        {
            if (_followToday) Anchor = DateTime.Today;   // triggers ReloadRange
            else ReloadRange();
        }

        /// <summary>Snapshot of what the detail window shows, for the exporters.</summary>
        public ReportData BuildReport()
        {
            // Clones: the report is generated on a worker thread while the live record keeps changing.
            var days = DaysWithLiveToday().Select(d => d.Clone()).ToList();
            return ReportBuilder.Build(Period, _range, days, DateTime.Now, GoalSeconds, ReportTitle);
        }
    }
}
