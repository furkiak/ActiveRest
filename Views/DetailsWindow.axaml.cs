using ActiveRest.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Platform.Storage;

namespace ActiveRest.Views
{
    public partial class DetailsWindow : Window
    {
        public DetailsWindow()
        {
            InitializeComponent();
        } 
        private async void ExportToPdf_Click(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainWindowViewModel vm)
            {
                try
                {
                    var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Export activity summary",
                        SuggestedFileName = $"ActiveRest-activity-{DateTime.Now:yyyyMMdd}.pdf",
                        DefaultExtension = "pdf"
                    });
                    if (file == null) return;
                    vm.ForceSave();
                    var pdfService = new ActiveRest.Services.PdfExportService();
                    pdfService.ExportReport(vm, file.Path.LocalPath);
                    this.FindControl<TextBlock>("ExportFeedbackText")!.Text = "PDF saved to your chosen location.";
                }
                catch (Exception ex)
                {
                    this.FindControl<TextBlock>("ExportFeedbackText")!.Text = $"PDF export failed: {ex.Message}";
                }
            }
        }
        
    }
}
