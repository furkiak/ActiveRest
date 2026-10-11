using ActiveRest.Localization;
using ActiveRest.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ActiveRest.ViewModels
{
    public enum ExportFormat
    {
        Pdf,
        Csv
    }

    public partial class DetailsViewModel : ViewModelBase
    {
        private readonly PdfExportService _pdf;
        private readonly CsvExportService _csv;
        private readonly Loc _loc = Loc.Instance;
        private string? _lastPath;

        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isError;
        [ObservableProperty] private bool _canReveal;

        public MainWindowViewModel Main { get; }
        public bool HasStatus => !string.IsNullOrEmpty(StatusText);

        public DetailsViewModel(MainWindowViewModel main, PdfExportService pdf, CsvExportService csv)
        {
            Main = main;
            _pdf = pdf;
            _csv = csv;
        }

        partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatus));

        public string SuggestedFileName(ExportFormat format)
        {
            string ext = format == ExportFormat.Pdf ? "pdf" : "csv";
            return $"ActiveRest_{Main.Period}_{DateTime.Now:yyyyMMdd_HHmm}.{ext}";
        }

        public async Task ExportAsync(ExportFormat format, string path)
        {
            if (IsBusy) return;
            IsBusy = true;
            IsError = false;
            CanReveal = false;
            StatusText = _loc.Get("export.working");

            try
            {
                var data = Main.BuildReport();
                await Task.Run(() =>
                {
                    if (format == ExportFormat.Pdf) _pdf.Export(data, path, _loc);
                    else _csv.Export(data, path, _loc);
                });

                _lastPath = path;
                CanReveal = true;
                StatusText = _loc.Format("export.saved", Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                Logger.Error("Export failed", ex);
                IsError = true;
                StatusText = _loc.Format("export.failed", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Reveal()
        {
            if (_lastPath != null) ShellHelper.RevealInFolder(_lastPath);
        }
    }
}
