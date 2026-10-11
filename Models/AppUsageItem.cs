using CommunityToolkit.Mvvm.ComponentModel;

namespace ActiveRest.Models
{
    /// <summary>One row of the per-application breakdown. Observable so lists can be updated in place.</summary>
    public partial class AppUsageItem : ObservableObject
    {
        [ObservableProperty] private string _appKey = string.Empty;
        [ObservableProperty] private string _appName = string.Empty;
        [ObservableProperty] private string _initial = "?";
        [ObservableProperty] private string _durationText = string.Empty;
        [ObservableProperty] private string _percentageText = string.Empty;
        [ObservableProperty] private double _totalSeconds;

        /// <summary>Share of the period's tracked app time, 0-1. Drives the capsule bar width.</summary>
        [ObservableProperty] private double _fraction;

        /// <summary>Hue (0-360) derived from the app name so each app keeps a stable accent colour.</summary>
        [ObservableProperty] private double _hue;
    }
}
