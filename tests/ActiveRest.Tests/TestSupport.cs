using ActiveRest.Services;
using System;
using System.IO;

namespace ActiveRest.Tests
{
    /// <summary>Temp folder that is removed when the test finishes.</summary>
    internal sealed class TempFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "activerest-tests-" + Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }

    internal sealed class FakeTracker : IActivityTracker
    {
        public double Idle { get; set; }
        public bool Media { get; set; }
        public string App { get; set; } = "Code";

        public double GetIdleTimeSeconds() => Idle;
        public bool IsMediaPlaying() => Media;
        public string GetActiveAppName() => App;
    }

    internal sealed class FakeClock
    {
        public DateTime Now { get; set; }
        public FakeClock(DateTime start) => Now = start;
        public DateTime Read() => Now;
        public void Advance(double seconds) => Now = Now.AddSeconds(seconds);
    }
}
