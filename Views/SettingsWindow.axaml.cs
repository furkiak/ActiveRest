using Avalonia.Controls;
using Avalonia.Interactivity;
using ActiveRest.Services;
using ActiveRest.Models;
using System;
using System.Diagnostics;
using ActiveRest.ViewModels;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace ActiveRest.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly DataManager _dataManager;
        private readonly SettingsService _startupService;
        private MainWindowViewModel? _viewModel;
        private AppSettings _settings;
        private int _currentInterval;
        private bool _initialAutoStart;

        public SettingsWindow() : this(new DataManager(), null) { }

        public SettingsWindow(MainWindowViewModel viewModel) : this(viewModel.DataStore, viewModel) { }

        private SettingsWindow(DataManager dataManager, MainWindowViewModel? viewModel)
        {
            _dataManager = dataManager;
            _viewModel = viewModel;
            _startupService = new SettingsService(_dataManager.Profile);
            InitializeComponent();
            _settings = _dataManager.LoadSettings();
            _currentInterval = _settings.EyeRestIntervalMinutes;

            UpdateIntervalDisplay();

            var autoStartControl = this.FindControl<CheckBox>("AutoStartCheck");
            if (autoStartControl != null)
            {
                _initialAutoStart = _startupService.IsAutoStartEnabled();
                autoStartControl.IsChecked = _initialAutoStart;
                autoStartControl.IsEnabled = _startupService.CanSetAutoStart;
            }
            if (_dataManager.Profile.IsSeparate)
            {
                Title = "ActiveRest settings · separate profile";
                this.FindControl<TextBlock>("StartupDescription")!.Text = "Unavailable for this separate profile; the normal Windows startup entry stays unchanged.";
            }
            this.FindControl<TextBlock>("ProfileDescription")!.Text = (_dataManager.Profile.IsSeparate
                ? "Separate profile · " : "Data folder · ") + _dataManager.DataFolder;
            this.FindControl<CheckBox>("EyeRestCheck")!.IsChecked = _settings.IsEyeRestEnabled;
            this.FindControl<CheckBox>("SundayCheck")!.IsChecked = _settings.WeekStartsOnSunday;
            this.FindControl<ComboBox>("ThemeChoice")!.SelectedIndex = _settings.AppearanceTheme == "Dark" ? 1 : 0;
            this.FindControl<ComboBox>("TextSizeChoice")!.SelectedIndex = _settings.TextScalePercent switch { 150 => 1, 200 => 2, _ => 0 };
            if (_dataManager.SettingsRecoveryNotice != null) ShowFeedback(_dataManager.SettingsRecoveryNotice);
        }

        private void Minus_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentInterval > 5) { _currentInterval -= 5; UpdateIntervalDisplay(); }
        }

        private void Plus_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentInterval < 120) { _currentInterval += 5; UpdateIntervalDisplay(); }
        }

        private void UpdateIntervalDisplay()
        {
            var textBlock = this.FindControl<TextBlock>("IntervalValueText");
            if (textBlock != null) textBlock.Text = $"{_currentInterval} minutes";
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _settings.EyeRestIntervalMinutes = _currentInterval;
                _settings.IsEyeRestEnabled = this.FindControl<CheckBox>("EyeRestCheck")!.IsChecked == true;
                _settings.WeekStartsOnSunday = this.FindControl<CheckBox>("SundayCheck")!.IsChecked == true;
                _settings.AppearanceTheme = this.FindControl<ComboBox>("ThemeChoice")!.SelectedIndex == 1 ? "Dark" : "Light";
                _settings.TextScalePercent = this.FindControl<ComboBox>("TextSizeChoice")!.SelectedIndex switch { 1 => 150, 2 => 200, _ => 100 };
                _dataManager.SaveSettings(_settings);
                AppearanceService.Apply(_settings);
                _viewModel?.ReloadSettings();
                var autoStartControl = this.FindControl<CheckBox>("AutoStartCheck");
                var requestedAutoStart = autoStartControl?.IsChecked ?? false;
                if (_startupService.CanSetAutoStart && requestedAutoStart != _initialAutoStart)
                    _startupService.SetAutoStart(requestedAutoStart);
                this.Close();
            }
            catch (Exception ex) { ShowFeedback($"Could not save all settings: {ex.Message}"); }
        }

        private async void Export_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export ActiveRest history",
                    SuggestedFileName = $"ActiveRest-history-{DateTime.Now:yyyyMMdd}.json",
                    DefaultExtension = "json"
                });
                if (file == null) return;
                if (_viewModel == null) throw new InvalidOperationException("Open the main window first.");
                _viewModel.ExportHistory(file.Path.LocalPath);
                ShowFeedback("History exported to the selected JSON file.");
            }
            catch (Exception ex) { ShowFeedback($"Export failed: {ex.Message}"); }
        }

        private async void Delete_Click(object? sender, RoutedEventArgs e)
        {
            var prompt = new Window
            {
                Title = "Delete local history?", Width = 420, MinWidth = 320, MaxHeight = 650,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            prompt.Content = new ScrollViewer { Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20), Spacing = 15,
                    Children =
                    {
                        new TextBlock { Text = "Delete all local activity history and its backup? Export first if you want to keep a copy.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new WrapPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                NewChoice("Cancel", false), NewChoice("Delete history", true)
                            }
                        }
                    }
                } };
            if (await prompt.ShowDialog<bool>(this))
            {
                try
                {
                    if (_viewModel == null) throw new InvalidOperationException("Open the main window first.");
                    _viewModel.DeleteHistory();
                    ShowFeedback("Local history and backup deleted.");
                }
                catch (Exception ex) { ShowFeedback($"Delete failed: {ex.Message}"); }
            }

            Button NewChoice(string label, bool answer)
            {
                var button = new Button { Content = label, Padding = new Avalonia.Thickness(12, 7), Margin = new Avalonia.Thickness(0, 0, 10, 8) };
                button.Click += (_, _) => prompt.Close(answer);
                return button;
            }
        }

        private void OpenDataFolder_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var process = new ProcessStartInfo("explorer.exe");
                process.ArgumentList.Add(_viewModel?.DataFolder ?? _dataManager.DataFolder);
                Process.Start(process);
            }
            catch (Exception ex) { ShowFeedback($"Could not open the folder: {ex.Message}"); }
        }

        private void ShowFeedback(string text) => this.FindControl<TextBlock>("FeedbackText")!.Text = text;
    }
}
