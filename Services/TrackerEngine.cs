using ActiveRest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ActiveRest.Services
{
    public class TrackerEngine
    {
        private readonly IActivityTracker _activityTracker;
        private readonly DataManager _dataManager;
        private Timer _timer;
        private byte ModeTime =5;
        public DailyStat CurrentDayStat { get; private set; }
        private int _continuousFocusSeconds = 0;  
        private AppSettings _settings;
        public TrackerEngine(IActivityTracker activityTracker, DataManager dataManager)
        {
            _activityTracker = activityTracker;
            _dataManager = dataManager;
            _settings = _dataManager.LoadSettings(); 
            LoadTodayStat();
        }

        private void LoadTodayStat()
        {
            var allData = _dataManager.LoadAllData(); 
            CurrentDayStat = allData.Find(d => d.Date.Date == DateTime.Today.Date) ?? new DailyStat();
        }
        
        public void StartTracking()
        {
            _timer = new Timer(1000);  
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (DateTime.Today != CurrentDayStat.Date.Date)
                {
                    LoadTodayStat();
                }

                double idleTime = _activityTracker.GetIdleTimeSeconds();
                bool isMediaPlaying = _activityTracker.IsMediaPlaying();

                if (idleTime < 180)
                {
                    CurrentDayStat.TotalFocusedSeconds++;
                    _continuousFocusSeconds++;  
                     
                    string activeApp = _activityTracker.GetActiveAppName();
                    if (CurrentDayStat.AppUsageSeconds.ContainsKey(activeApp))
                        CurrentDayStat.AppUsageSeconds[activeApp]++;
                    else
                        CurrentDayStat.AppUsageSeconds.Add(activeApp, 1);
                     
                    if (_settings.IsEyeRestEnabled && _continuousFocusSeconds >= (_settings.EyeRestIntervalMinutes * 60))
                    { 
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var toast = new Views.NotificationWindow();
                            toast.Show();
                        });

                        _continuousFocusSeconds = 0;  
                    }
                }
                else if (idleTime >= 180 && isMediaPlaying)
                {
                    CurrentDayStat.TotalRelaxedSeconds++;
                    _continuousFocusSeconds = 0;  
                }
                else
                {
                    CurrentDayStat.TotalBreakSeconds++;
                    _continuousFocusSeconds = 0;  
                }

                if ((CurrentDayStat.TotalFocusedSeconds + CurrentDayStat.TotalRelaxedSeconds + CurrentDayStat.TotalBreakSeconds) % 60 == 0)
                {
                    _dataManager.SaveOrUpdateToday(CurrentDayStat);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error-6: {ex.Message}");
            }
        }
    }
}
