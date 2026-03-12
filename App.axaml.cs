using ActiveRest.ViewModels;
using ActiveRest.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;

namespace ActiveRest
{
    public partial class App : Application
    {
       
        public override void Initialize()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            AvaloniaXamlLoader.Load(this);
        }
        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow is ActiveRest.Views.MainWindow mainWindow)
                {
                    mainWindow.Show();
                    mainWindow.PositionWindowToBottomRight();  
                    mainWindow.Activate();
                }
            }
        }
  
        private void TrayShow_Click(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow is ActiveRest.Views.MainWindow mainWindow)
                {
                    mainWindow.Show();
                    mainWindow.PositionWindowToBottomRight();
                    mainWindow.Activate();
                }
            }
        }

    
        private void TrayExit_Click(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            { 
                if (desktop.MainWindow?.DataContext is ActiveRest.ViewModels.MainWindowViewModel vm)
                {
                    vm.ForceSave();
                }

                desktop.Shutdown();
            }
        }
        public override void OnFrameworkInitializationCompleted()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            { 
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        { 
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
             
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}