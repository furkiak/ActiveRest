using Avalonia;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace ActiveRest.Views
{
    public partial class MainWindow : Window
    { 
        private bool _isModalOpen = false; 
        public MainWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened;
        } 
        private void OnWindowOpened(object? sender, EventArgs e)
        {
            PositionWindowToBottomRight();
        } 
        public void PositionWindowToBottomRight()
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                var scaling = screen.Scaling;
                var windowWidth = this.Width * scaling;
                var windowHeight = this.Height * scaling;

                int x = (int)(workArea.Right - windowWidth - 10);
                int y = (int)(workArea.Bottom - windowHeight - 10);

                this.Position = new PixelPoint(x, y);
            }
        }
         
        private async void OpenDetails_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _isModalOpen = true;  

            var detailsWin = new DetailsWindow
            {
                DataContext = this.DataContext
            };
             
            await detailsWin.ShowDialog(this);

            _isModalOpen = false; 
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        { 
            if (!_isModalOpen)
            {
                this.Hide();
            }
        }

        private async void OpenSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _isModalOpen = true;
            var settingsWin = new SettingsWindow();
            await settingsWin.ShowDialog(this);
            _isModalOpen = false; 
        }
    }
}