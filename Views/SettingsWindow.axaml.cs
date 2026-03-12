using Avalonia.Controls;
using Avalonia.Interactivity;
using ActiveRest.Services;
using ActiveRest.Models;
using System;

namespace ActiveRest.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly DataManager _dataManager = new();
        private readonly SettingsService _startupService = new();
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
            if (textBlock != null) textBlock.Text = $"{_currentInterval} Minute";
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            _settings.EyeRestIntervalMinutes = _currentInterval;
            _dataManager.SaveSettings(_settings);

            var autoStartControl = this.FindControl<CheckBox>("AutoStartCheck");
            _startupService.SetAutoStart(autoStartControl?.IsChecked ?? false);

            this.Close();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => this.Close();
    }
}