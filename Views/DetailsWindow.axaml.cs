using ActiveRest.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;

namespace ActiveRest.Views
{
    public partial class DetailsWindow : Window
    {
        public DetailsWindow()
        {
            InitializeComponent();
            Opened += (_, _) => WindowPlacement.ClampToWorkArea(this);
        }

        private void Close_Click(object? sender, RoutedEventArgs e) => Close();

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private async void ExportPdf_Click(object? sender, RoutedEventArgs e) => await ExportAsync(ExportFormat.Pdf);

        private async void ExportCsv_Click(object? sender, RoutedEventArgs e) => await ExportAsync(ExportFormat.Csv);

        private async Task ExportAsync(ExportFormat format)
        {
            if (DataContext is not DetailsViewModel vm) return;

            bool pdf = format == ExportFormat.Pdf;
            var options = new FilePickerSaveOptions
            {
                Title = pdf ? "PDF" : "CSV",
                SuggestedFileName = vm.SuggestedFileName(format),
                DefaultExtension = pdf ? "pdf" : "csv",
                ShowOverwritePrompt = true,
                SuggestedStartLocation = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(pdf ? "PDF" : "CSV") { Patterns = new[] { pdf ? "*.pdf" : "*.csv" } }
                }
            };

            var file = await StorageProvider.SaveFilePickerAsync(options);
            string? path = file?.TryGetLocalPath();
            if (path != null) await vm.ExportAsync(format, path);
        }
    }
}
