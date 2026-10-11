using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ActiveRest.ViewModels;
using System;
using System.Linq;

namespace ActiveRest.Views
{
    /// <summary>
    /// Eye-rest reminder. Shown without taking focus (ShowActivated=False) so it never swallows keystrokes
    /// that were meant for the window the user is typing in.
    /// </summary>
    public partial class ToastWindow : Window
    {
        private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);
        private readonly DispatcherTimer _timer = new() { Interval = Step };
        private bool _closing;

        public ToastWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
            Closed += (_, _) => _timer.Stop();
            _timer.Tick += (_, _) => (DataContext as ToastViewModel)?.Advance(Step.TotalSeconds);
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
            if (screen != null)
            {
                double scale = screen.Scaling;
                var work = screen.WorkingArea;
                int x = work.Right - (int)Math.Ceiling(Width * scale);
                int y = work.Y + (int)(8 * scale);
                Position = new PixelPoint(x, y);
            }

            DispatcherTimer.RunOnce(() => Root.Classes.Set("hidden", false), TimeSpan.FromMilliseconds(30));
            _timer.Start();
        }

        /// <summary>Fades the toast out and closes it.</summary>
        public void Dismiss()
        {
            if (_closing) return;
            _closing = true;
            _timer.Stop();
            Root.Classes.Set("hidden", true);
            DispatcherTimer.RunOnce(Close, TimeSpan.FromMilliseconds(260));
        }
    }
}
