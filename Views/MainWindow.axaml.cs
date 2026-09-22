using Avalonia.Controls;
using ActiveRest.ViewModels;

namespace ActiveRest.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenDetails_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var detailsWindow = new DetailsWindow { DataContext = DataContext };
            await detailsWindow.ShowDialog(this);
        }

        private async void OpenSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow((MainWindowViewModel)DataContext!);
            await settingsWindow.ShowDialog(this);
        }
    }
}
