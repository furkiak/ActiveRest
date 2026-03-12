using ActiveRest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveRest.Services
{
    public class DataManager
    {
        private readonly string _dataFolder;
        private readonly string _filePath; 
        public DataManager()
        { 
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataFolder = Path.Combine(appData, "ActiveRest");
            _filePath = Path.Combine(_dataFolder, "stats.json"); 
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dataFolder))
                    Directory.CreateDirectory(_dataFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error-1: {ex.Message}");
            }
        }

        public List<DailyStat> LoadAllData()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<DailyStat>();

                string json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<DailyStat>>(json) ?? new List<DailyStat>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error-2: {ex.Message}");
                return new List<DailyStat>();
            }
        }

        public void SaveOrUpdateToday(DailyStat currentStat)
        {
            try
            {
                var allData = LoadAllData();
                var existing = allData.FirstOrDefault(d => d.Date.Date == currentStat.Date.Date);

                if (existing != null)
                { 
                    existing.TotalFocusedSeconds = currentStat.TotalFocusedSeconds;
                    existing.TotalRelaxedSeconds = currentStat.TotalRelaxedSeconds;
                    existing.TotalBreakSeconds = currentStat.TotalBreakSeconds; 
                    existing.AppUsageSeconds = currentStat.AppUsageSeconds;
                }
                else
                {
                    allData.Add(currentStat);
                }

                string json = JsonConvert.SerializeObject(allData, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error-3: {ex.Message}");
            }
        } 
        private string SettingsFilePath => Path.Combine(_dataFolder, "settings.json");

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath)) return new AppSettings();
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        } 
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error-4: {ex.Message}");
            }
        }
    }
}
