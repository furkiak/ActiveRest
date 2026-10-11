using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ActiveRest.Services
{
    /// <summary>Win32 implementation: input idle time, foreground process and audio activity.</summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsActivityTracker : IActivityTracker, IDisposable
    {
        private const float AudioPeakThreshold = 0.005f;
        private static readonly TimeSpan DeviceRefreshInterval = TimeSpan.FromSeconds(30);
        private const int MaxCachedProcesses = 256;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private readonly Dictionary<uint, string> _processNames = new();
        private MMDeviceEnumerator? _enumerator;
        private MMDevice? _device;
        private DateTime _deviceAcquiredAt = DateTime.MinValue;

        public double GetIdleTimeSeconds()
        {
            try
            {
                var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
                if (GetLastInputInfo(ref info))
                {
                    // Both counters wrap at 2^32 ms; unsigned subtraction stays correct across the wrap.
                    uint idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
                    return idleMs / 1000.0;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Idle time query failed", ex);
            }
            return 0;
        }

        public string GetActiveAppName()
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return "System Idle";

                GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == 0) return "Unknown";

                if (_processNames.TryGetValue(processId, out var cached)) return cached;

                string name = ResolveProcessName(processId);
                if (_processNames.Count >= MaxCachedProcesses) _processNames.Clear();
                _processNames[processId] = name;
                return name;
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string ResolveProcessName(uint processId)
        {
            try
            {
                using var process = Process.GetProcessById((int)processId);
                string name = process.ProcessName;
                return string.IsNullOrEmpty(name) ? "Unknown" : char.ToUpperInvariant(name[0]) + name[1..];
            }
            catch
            {
                return "Unknown";
            }
        }

        public bool IsMediaPlaying()
        {
            try
            {
                var device = GetPlaybackDevice();
                return device != null && device.AudioMeterInformation.MasterPeakValue > AudioPeakThreshold;
            }
            catch
            {
                // The default device changed or was removed; reacquire it on the next call.
                ReleaseDevice();
                return false;
            }
        }

        private MMDevice? GetPlaybackDevice()
        {
            if (_device != null && DateTime.UtcNow - _deviceAcquiredAt < DeviceRefreshInterval)
                return _device;

            ReleaseDevice();
            _enumerator ??= new MMDeviceEnumerator();
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _deviceAcquiredAt = DateTime.UtcNow;
            return _device;
        }

        private void ReleaseDevice()
        {
            _device?.Dispose();
            _device = null;
        }

        public void Dispose()
        {
            ReleaseDevice();
            _enumerator?.Dispose();
            _enumerator = null;
        }
    }
}
