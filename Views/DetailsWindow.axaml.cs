using ActiveRest.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ActiveRest.Views
{
    public partial class DetailsWindow : Window
    {
        public DetailsWindow()
        {
            InitializeComponent();
        } 
        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }
        private void ExportToPdf_Click(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainWindowViewModel vm)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string fileName = $"ActiveRest_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    string fullPath = Path.Combine(desktopPath, fileName);
                     
                    var pdfService = new ActiveRest.Services.PdfExportService();
                    pdfService.ExportReport(vm, fullPath);
                     
                    Process.Start("explorer.exe", $"/select,\"{fullPath}\"");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error-10: " + ex.Message);
                }
            }
        }
        
    }
}