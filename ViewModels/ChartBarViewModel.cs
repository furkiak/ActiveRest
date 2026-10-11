using CommunityToolkit.Mvvm.ComponentModel;

namespace ActiveRest.ViewModels
{
    public partial class ChartBarViewModel : ObservableObject
    {
        [ObservableProperty] private string _label = string.Empty;
        [ObservableProperty] private bool _showLabel;
        [ObservableProperty] private double _height = 3;
        [ObservableProperty] private bool _isCurrent;
        [ObservableProperty] private bool _metGoal;
        [ObservableProperty] private bool _isEmpty = true;
        [ObservableProperty] private string _tooltip = string.Empty;
    }
}
