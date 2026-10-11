using ActiveRest.Services;
using ActiveRest.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveRest.Views
{
    public partial class MainWindow : Window
    {
        private const double DesiredHeight = 744;

        private bool _dialogOpen;
        private bool _flyoutOpen;
        private bool _allowClose;

        /// <summary>Services needed to open the detail and settings windows.</summary>
        public AppServices? Services { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
            Opened += (_, _) => AttachFlyoutGuards();
        }

        private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

        // ================= show / hide =================

        public void ShowFromTray()
        {
            if (!IsVisible)
            {
                PositionWindow();
                Root.Classes.Set("hidden", true);
                Show();
            }

            if (Vm != null)
            {
                Vm.IsWindowVisible = true;
                Vm.RefreshNow();   // numbers may be minutes old: show current ones on the very first frame
            }
            Activate();
            DispatcherTimer.RunOnce(() => Root.Classes.Set("hidden", false), TimeSpan.FromMilliseconds(30));
        }

        public void HideAnimated()
        {
            if (!IsVisible) return;
            Root.Classes.Set("hidden", true);
            DispatcherTimer.RunOnce(() =>
            {
                if (Root.Classes.Contains("hidden"))
                {
                    if (Vm != null) Vm.IsWindowVisible = false;
                    Hide();
                }
            }, TimeSpan.FromMilliseconds(220));
        }

        /// <summary>Really closes the window (application shutdown); a normal close request only hides it.</summary>
        public void CloseForShutdown()
        {
            _allowClose = true;
            Close();
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_allowClose) return;
            e.Cancel = true;
            HideAnimated();
        }

        private void PositionWindow()
        {
            var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
            if (screen == null) return;

            double scale = screen.Scaling;
            var work = screen.WorkingArea;
            Height = Math.Min(DesiredHeight, work.Height / scale - 8);

            int x = work.Right - (int)Math.Ceiling(Width * scale);
            int y = work.Bottom - (int)Math.Ceiling(Height * scale);
            Position = new PixelPoint(Math.Max(work.X, x), Math.Max(work.Y, y));
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (_dialogOpen || _flyoutOpen || Vm is { IsPinned: true }) return;
            HideAnimated();
        }

        private void AttachFlyoutGuards()
        {
            if (DateButton.Flyout is not FlyoutBase flyout) return;
            flyout.Opened += (_, _) => _flyoutOpen = true;
            flyout.Closed += (_, _) => _flyoutOpen = false;
        }

        // ================= input =================

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    HideAnimated();
                    e.Handled = true;
                    break;
                case Key.Left when Vm?.PreviousCommand.CanExecute(null) == true:
                    Vm.PreviousCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Right when Vm?.NextCommand.CanExecute(null) == true:
                    Vm.NextCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        private void OnCalendarSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            DateButton.Flyout?.Hide();
        }

        // ================= dialogs =================

        private async void OpenDetails_Click(object? sender, RoutedEventArgs e)
        {
            if (Vm == null || Services == null) return;
            _dialogOpen = true;
            try
            {
                var window = new DetailsWindow { DataContext = new DetailsViewModel(Vm, Services.Pdf, Services.Csv) };
                await window.ShowDialog(this);
            }
            finally
            {
                _dialogOpen = false;
            }
        }

        private async void OpenSettings_Click(object? sender, RoutedEventArgs e) => await OpenSettingsAsync();

        public async Task OpenSettingsAsync()
        {
            if (Services == null) return;
            if (!IsVisible) ShowFromTray();

            _dialogOpen = true;
            var vm = new SettingsViewModel(Services.Settings, Services.Startup);
            try
            {
                var window = new SettingsWindow { DataContext = vm };
                await window.ShowDialog(this);
            }
            finally
            {
                vm.Dispose();
                _dialogOpen = false;
            }
        }
    }
}
