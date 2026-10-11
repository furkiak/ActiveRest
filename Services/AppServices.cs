using ActiveRest.Localization;
using System;

namespace ActiveRest.Services
{
    /// <summary>Composition root: builds the long-lived services once and hands them to the view models.</summary>
    public sealed class AppServices
    {
        public DataManager Data { get; }
        public SettingsStore Settings { get; }
        public IActivityTracker Tracker { get; }
        public TrackerEngine Engine { get; }
        public StartupService Startup { get; } = new();
        public PdfExportService Pdf { get; } = new();
        public CsvExportService Csv { get; } = new();
        public Loc Loc => Localization.Loc.Instance;

        public AppServices(IActivityTracker? tracker = null, string? dataFolder = null)
        {
            Data = new DataManager(dataFolder);
            Settings = new SettingsStore(dataFolder);
            Tracker = tracker ?? ActivityTrackerFactory.Create();
            Engine = new TrackerEngine(Tracker, Data, Settings);
        }

        /// <summary>Persists everything synchronously and releases native resources.</summary>
        public void Shutdown()
        {
            Engine.SaveBlocking();
            (Tracker as IDisposable)?.Dispose();
        }
    }
}
