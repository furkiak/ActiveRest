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
        private readonly DataManager _dataManager = new();
        private readonly SettingsService _startupService = new();
        private MainWindowViewModel? _viewModel;
        private AppSettings _settings;
        private int _currentInterval;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = _dataManager.LoadSettings();
            _currentInterval = _settings.EyeRestIntervalMinutes;

            UpdateIntervalDisplay();

            var autoStartControl = this.FindControl<CheckBox>("AutoStartCheck");
            if (autoStartControl != null)
                autoStartControl.IsChecked = _startupService.IsAutoStartEnabled();
            this.FindControl<CheckBox>("EyeRestCheck")!.IsChecked = _settings.IsEyeRestEnabled;
            this.FindControl<CheckBox>("SundayCheck")!.IsChecked = _settings.WeekStartsOnSunday;
        }

        public SettingsWindow(MainWindowViewModel viewModel) : this() => _viewModel = viewModel;

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
            if (textBlock != null) textBlock.Text = $"{_currentInterval} Minute";
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _settings.EyeRestIntervalMinutes = _currentInterval;
                _settings.IsEyeRestEnabled = this.FindControl<CheckBox>("EyeRestCheck")!.IsChecked == true;
                _settings.WeekStartsOnSunday = this.FindControl<CheckBox>("SundayCheck")!.IsChecked == true;
                _dataManager.SaveSettings(_settings);
                _viewModel?.ReloadSettings();
                var autoStartControl = this.FindControl<CheckBox>("AutoStartCheck");
                _startupService.SetAutoStart(autoStartControl?.IsChecked ?? false);
                this.Close();
            }
            catch (Exception ex) { ShowFeedback($"Could not save all settings: {ex.Message}"); }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => this.Close();

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
                Title = "Delete local history?", Width = 380, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            prompt.Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20), Spacing = 15,
                    Children =
                    {
                        new TextBlock { Text = "Delete all local activity history and its backup? Export first if you want to keep a copy.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal, Spacing = 10,
                            Children =
                            {
                                NewChoice("Cancel", false), NewChoice("Delete history", true)
                            }
                        }
                    }
                };
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
                var button = new Button { Content = label, Padding = new Avalonia.Thickness(12, 7) };
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
