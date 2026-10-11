using ActiveRest.Localization;
using ActiveRest.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ActiveRest.ViewModels
{
    /// <summary>Settings apply live: every edit is written to the shared store, which the rest of the app observes.</summary>
    public partial class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly SettingsStore _store;
        private readonly StartupService _startup;
        private readonly Loc _loc = Loc.Instance;
        private readonly bool _loading;

        [ObservableProperty] private int _languageIndex;
        [ObservableProperty] private int _themeIndex;
        [ObservableProperty] private bool _eyeEnabled;
        [ObservableProperty] private double _eyeInterval;
        [ObservableProperty] private double _goalHours;
        [ObservableProperty] private double _idleMinutes;
        [ObservableProperty] private bool _startWithWindows;

        public bool IsStartupSupported => _startup.IsSupported;
        public string DataFolder => AppPaths.DataFolder;

        public string EyeIntervalText => _loc.Format("eye.interval", (int)EyeInterval);
        public string GoalText => _loc.FormatGoal(GoalHours * 3600);
        public string IdleText => _loc.Format("focus.idle.value", (int)IdleMinutes);
        public string VersionText => _loc.Format("settings.version", typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "2.0.0");

        public SettingsViewModel(SettingsStore store, StartupService startup)
        {
            _store = store;
            _startup = startup;

            _loading = true;
            var s = store.Current;
            LanguageIndex = s.Language switch { "en" => 1, "tr" => 2, _ => 0 };
            ThemeIndex = s.Theme switch { "light" => 1, "dark" => 2, _ => 0 };
            EyeEnabled = s.IsEyeRestEnabled;
            EyeInterval = s.EyeRestIntervalMinutes;
            GoalHours = s.DailyFocusGoalMinutes / 60.0;
            IdleMinutes = s.IdleThresholdMinutes;
            StartWithWindows = startup.IsAutoStartEnabled();
            _loading = false;

            _loc.LanguageChanged += RefreshTexts;
        }

        partial void OnLanguageIndexChanged(int value)
        {
            if (_loading || value < 0) return;
            _store.Update(s => s.Language = value switch { 1 => "en", 2 => "tr", _ => "auto" });
        }

        partial void OnThemeIndexChanged(int value)
        {
            if (_loading || value < 0) return;
            _store.Update(s => s.Theme = value switch { 1 => "light", 2 => "dark", _ => "system" });
        }

        partial void OnEyeEnabledChanged(bool value)
        {
            if (!_loading) _store.Update(s => s.IsEyeRestEnabled = value);
        }

        partial void OnEyeIntervalChanged(double value)
        {
            OnPropertyChanged(nameof(EyeIntervalText));
            if (!_loading) _store.Update(s => s.EyeRestIntervalMinutes = (int)Math.Round(value));
        }

        partial void OnGoalHoursChanged(double value)
        {
            OnPropertyChanged(nameof(GoalText));
            if (!_loading) _store.Update(s => s.DailyFocusGoalMinutes = (int)Math.Round(value * 60));
        }

        partial void OnIdleMinutesChanged(double value)
        {
            OnPropertyChanged(nameof(IdleText));
            if (!_loading) _store.Update(s => s.IdleThresholdMinutes = (int)Math.Round(value));
        }

        partial void OnStartWithWindowsChanged(bool value)
        {
            if (!_loading) _startup.SetAutoStart(value);
        }

        [RelayCommand]
        private void OpenDataFolder() => ShellHelper.OpenFolder(AppPaths.DataFolder);

        private void RefreshTexts()
        {
            OnPropertyChanged(nameof(EyeIntervalText));
            OnPropertyChanged(nameof(GoalText));
            OnPropertyChanged(nameof(IdleText));
            OnPropertyChanged(nameof(VersionText));
        }

        public void Dispose() => _loc.LanguageChanged -= RefreshTexts;
    }
}
