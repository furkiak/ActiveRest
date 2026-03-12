using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ActiveRest.Services
{
 
        public class WindowsActivityTracker : IActivityTracker
    { 
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetActiveAppName()
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow(); 
                if (hWnd == IntPtr.Zero) return "System Idle";
                 
                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                 
                string appName = process.ProcessName;
                if (string.IsNullOrEmpty(appName)) return "Unknown";

                return char.ToUpper(appName[0]) + appName.Substring(1);
            }
            catch
            {
                return "Unknown";
            }
        }
        [StructLayout(LayoutKind.Sequential)]
            struct LASTINPUTINFO
            {
                public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));
                [MarshalAs(UnmanagedType.U4)]
                public UInt32 cbSize;
                [MarshalAs(UnmanagedType.U4)]
                public UInt32 dwTime;
            }

            [DllImport("user32.dll")]
            static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

            public double GetIdleTimeSeconds()
            {
                try
                {
                    LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                    lastInputInfo.cbSize = (uint)LASTINPUTINFO.SizeOf;
                    lastInputInfo.dwTime = 0;

                    if (GetLastInputInfo(ref lastInputInfo))
                    {
                        uint envTicks = (uint)Environment.TickCount;
                        uint lastInputTick = lastInputInfo.dwTime;
                        uint idleTicks = envTicks - lastInputTick;

                        return idleTicks / 1000.0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error-7: {ex.Message}");
                }
                return 0;
            }
         
            public bool IsMediaPlaying()
            {
                try
                { 
                    using var enumerator = new MMDeviceEnumerator();
                    using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                    if (device != null)
                    { 
                        return device.AudioMeterInformation.MasterPeakValue > 0.005f;
                    }
                }
                catch
                { 
                    return false;
                }
                return false;
            }
        }
    }
