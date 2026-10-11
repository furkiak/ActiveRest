using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ActiveRest.ViewModels
{
    /// <summary>State of the 20-20-20 reminder: a countdown ring plus snooze/dismiss actions.</summary>
    public partial class ToastViewModel : ViewModelBase
    {
        private readonly double _totalSeconds;
        private double _remaining;

        [ObservableProperty] private int _secondsLeft;
        [ObservableProperty] private double _sweep = 359.9;

        public event Action? SnoozeRequested;
        public event Action? DismissRequested;
        public event Action? Finished;

        public ToastViewModel(int totalSeconds = 20)
        {
            _totalSeconds = totalSeconds;
            _remaining = totalSeconds;
            _secondsLeft = totalSeconds;
        }

        public void Advance(double elapsedSeconds)
        {
            _remaining = Math.Max(0, _remaining - elapsedSeconds);
            SecondsLeft = (int)Math.Ceiling(_remaining);
            Sweep = Math.Max(0.1, Math.Min(359.9, 360 * _remaining / _totalSeconds));
            if (_remaining <= 0) Finished?.Invoke();
        }

        [RelayCommand]
        private void Snooze() => SnoozeRequested?.Invoke();

        [RelayCommand]
        private void Dismiss() => DismissRequested?.Invoke();
    }
}
