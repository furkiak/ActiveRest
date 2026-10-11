using System;
using System.Threading;

namespace ActiveRest.Services
{
    /// <summary>
    /// Ensures only one tracker runs per user session; two instances would double-count every second and
    /// fight over the same data file. A second launch asks the running instance to show its window instead.
    /// </summary>
    public sealed class SingleInstanceGuard : IDisposable
    {
        private const string MutexName = @"Local\ActiveRest.SingleInstance";
        private const string ShowEventName = @"Local\ActiveRest.ShowWindow";

        private readonly Mutex? _mutex;
        private EventWaitHandle? _showEvent;
        private Thread? _listener;
        private volatile bool _disposed;

        public bool IsPrimary { get; }

        /// <summary>Raised on a background thread when another launch asked the running instance to show itself.</summary>
        public event Action? ShowRequested;

        public SingleInstanceGuard()
        {
            try
            {
                _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
                IsPrimary = createdNew;
                if (!createdNew)
                {
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
            catch (Exception ex)
            {
                // If the platform cannot create named mutexes, run anyway rather than refusing to start.
                Logger.Warn("Single-instance check unavailable", ex);
                IsPrimary = true;
            }
        }

        /// <summary>Starts listening for "show window" requests (Windows only; other platforms have no named events).</summary>
        public void StartListening()
        {
            if (!OperatingSystem.IsWindows() || !IsPrimary) return;
            try
            {
                _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
                _listener = new Thread(() =>
                {
                    while (!_disposed && _showEvent.WaitOne())
                    {
                        if (_disposed) break;
                        ShowRequested?.Invoke();
                    }
                })
                { IsBackground = true, Name = "ActiveRest.SingleInstance" };
                _listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Warn("Cannot listen for second launches", ex);
            }
        }

        /// <summary>Called by a secondary launch: wakes the primary instance.</summary>
        public static void SignalPrimary()
        {
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                using var handle = EventWaitHandle.OpenExisting(ShowEventName);
                handle.Set();
            }
            catch
            {
                // The primary instance is not listening (yet); nothing else to do.
            }
        }

        public void Dispose()
        {
            _disposed = true;
            try { _showEvent?.Set(); } catch { }
            _showEvent?.Dispose();
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
        }
    }
}
