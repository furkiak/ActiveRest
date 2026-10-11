using ActiveRest.Localization;
using ActiveRest.Services;
using ActiveRest.ViewModels;
using ActiveRest.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Linq;

namespace ActiveRest
{
    public partial class App : Application
    {
        /// <summary>Set by <see cref="Program"/> before the UI starts.</summary>
        public static SingleInstanceGuard? SingleInstance { get; set; }

        private AppServices? _services;
        private MainWindowViewModel? _vm;
        private MainWindow? _mainWindow;
        private DispatcherTimer? _ticker;
        private ToastWindow? _toast;
        private TrayIcon? _trayIcon;
        private NativeMenuItem? _openItem, _pauseItem, _settingsItem, _exitItem;
        private bool _shuttingDown;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // A tray application: closing windows must never end the process, only "Quit" does.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                DisableAvaloniaDataAnnotationValidation();

                Dispatcher.UIThread.UnhandledException += (_, e) =>
                {
                    Logger.Error("Unhandled UI exception", e.Exception);
                    e.Handled = true;
                };

                _services = new AppServices();
                ApplyAppearance();
                _services.Settings.Changed += ApplyAppearance;

                _vm = new MainWindowViewModel(_services.Engine, _services.Data, _services.Settings);
                _vm.PropertyChanged += OnViewModelPropertyChanged;
                _mainWindow = new MainWindow { DataContext = _vm, Services = _services };

                _services.Engine.EyeRestDue += OnEyeRestDue;
                _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _ticker.Tick += OnTick;
                _ticker.Start();

                BuildTray();

                if (SingleInstance != null)
                    SingleInstance.ShowRequested += () => Dispatcher.UIThread.Post(ShowMainWindow);

                desktop.Exit += (_, _) => Cleanup();

                bool startMinimized = desktop.Args?.Contains(StartupService.MinimizedArgument) == true;
                if (!startMinimized) ShowMainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        // ================= tick =================

        private void OnTick(object? sender, EventArgs e)
        {
            _services!.Engine.Tick();
            _vm!.OnTick();
        }

        // ================= appearance =================

        private void ApplyAppearance()
        {
            var settings = _services!.Settings.Current;
            Loc.Instance.SetLanguage(settings.Language);
            RequestedThemeVariant = settings.Theme switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        // ================= tray =================

        private void BuildTray()
        {
            _openItem = new NativeMenuItem();
            _openItem.Click += (_, _) => ShowMainWindow();
            _pauseItem = new NativeMenuItem();
            _pauseItem.Click += (_, _) => _vm!.SetPaused(!_vm.IsPaused);
            _settingsItem = new NativeMenuItem();
            _settingsItem.Click += async (_, _) => { if (_mainWindow != null) await _mainWindow.OpenSettingsAsync(); };
            _exitItem = new NativeMenuItem();
            _exitItem.Click += (_, _) => Exit();

            var menu = new NativeMenu
            {
                _openItem,
                _pauseItem,
                _settingsItem,
                new NativeMenuItemSeparator(),
                _exitItem
            };

            using var iconStream = AssetLoader.Open(new Uri("avares://ActiveRest/Assets/ActiveRest.ico"));
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconStream),
                Menu = menu,
                IsVisible = true
            };
            _trayIcon.Clicked += (_, _) => ShowMainWindow();
            TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });

            Loc.Instance.LanguageChanged += RefreshTrayTexts;
            RefreshTrayTexts();
        }

        private void RefreshTrayTexts()
        {
            var loc = Loc.Instance;
            if (_openItem == null || _vm == null) return;
            _openItem.Header = loc.Get("tray.open");
            _pauseItem!.Header = loc.Get(_vm.IsPaused ? "tray.resume" : "tray.pause");
            _settingsItem!.Header = loc.Get("tray.settings");
            _exitItem!.Header = loc.Get("tray.exit");
            if (_trayIcon != null) _trayIcon.ToolTipText = _vm.TrayTooltip;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.TrayTooltip):
                    if (_trayIcon != null) _trayIcon.ToolTipText = _vm!.TrayTooltip;
                    break;
                case nameof(MainWindowViewModel.IsPaused):
                    RefreshTrayTexts();
                    break;
            }
        }

        private void ShowMainWindow() => _mainWindow?.ShowFromTray();

        // ================= eye-rest toast =================

        private void OnEyeRestDue(object? sender, EyeRestDueEventArgs e)
        {
            if (_toast != null || _services == null) return;

            var toastVm = new ToastViewModel(e.BreakSeconds);
            var toast = new ToastWindow { DataContext = toastVm };
            toastVm.DismissRequested += toast.Dismiss;
            toastVm.Finished += toast.Dismiss;
            toastVm.SnoozeRequested += () =>
            {
                _services.Engine.Snooze(TimeSpan.FromMinutes(e.SnoozeMinutes));
                toast.Dismiss();
            };
            toast.Closed += (_, _) =>
            {
                if (ReferenceEquals(_toast, toast)) _toast = null;
            };

            _toast = toast;
            toast.Show();
        }

        // ================= lifecycle =================

        private void Exit()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;

            _toast?.Close();
            _mainWindow?.CloseForShutdown();
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }

        private void Cleanup()
        {
            _ticker?.Stop();
            _trayIcon?.Dispose();
            _services?.Shutdown();
        }

        private static void DisableAvaloniaDataAnnotationValidation()
        {
            var plugins = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
            foreach (var plugin in plugins)
                BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
