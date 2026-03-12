using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace ActiveRest.Views
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer _closeTimer;

        public NotificationWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened; 
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(7)
            };
            _closeTimer.Tick += (s, e) => { this.Close(); };
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                var scaling = screen.Scaling;
                 
                int x = (int)(workArea.X + workArea.Width - (this.Width * scaling) - 20);
                int y = (int)(workArea.Y + 20);

                this.Position = new PixelPoint(x, y);
            }
            _closeTimer.Start();
        }
    }
}