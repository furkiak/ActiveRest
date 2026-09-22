using ActiveRest.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActiveRest.Services;

public sealed class DataManager
{
    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly string _settingsPath;

    public string DataFolder { get; }
    public string? RecoveryNotice { get; private set; }
    public bool CanWrite { get; private set; } = true;

    public DataManager(string? dataFolder = null)
    {
        DataFolder = dataFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActiveRest");
        Directory.CreateDirectory(DataFolder);
        _filePath = Path.Combine(DataFolder, "stats.json");
        _settingsPath = Path.Combine(DataFolder, "settings.json");
    }

    public List<DailyStat> LoadAllData()
    {
        lock (_gate)
        {
            if (!CanWrite) return new List<DailyStat>();
            if (!File.Exists(_filePath))
            {
                if (!File.Exists(_filePath + ".bak")) return new List<DailyStat>();
                try { return RestoreBackup(); }
                catch (JsonException)
                {
                    CanWrite = false;
                    RecoveryNotice = "The local history backup is damaged. Tracking is paused so the file is not overwritten.";
                    return new List<DailyStat>();
                }
            }

            try { return ReadStats(_filePath); }
            catch (JsonException)
            {
                if (File.Exists(_filePath + ".bak"))
                {
                    try { return RestoreBackup(); }
                    catch (JsonException) { /* Both copies need manual recovery. */ }
                }

                CanWrite = false;
                RecoveryNotice = "The local history is damaged. Tracking is paused so the original file is not overwritten. Open the data folder to recover it.";
                return new List<DailyStat>();
            }
        }
    }

    private List<DailyStat> RestoreBackup()
    {
        var restored = ReadStats(_filePath + ".bak");
        if (File.Exists(_filePath))
        {
            var quarantine = _filePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            File.Move(_filePath, quarantine);
            RecoveryNotice = $"History was restored from a backup. The damaged copy is preserved as {Path.GetFileName(quarantine)}.";
        }
        else
        {
            RecoveryNotice = "History was restored from the backup after the main file was missing.";
        }
        File.Copy(_filePath + ".bak", _filePath);
        return restored;
    }

    private static List<DailyStat> ReadStats(string path)
    {
        var json = JToken.Parse(File.ReadAllText(path));
        if (json is not JArray array || array.Any(item => item is not JObject day ||
            day["Date"]?.Type != JTokenType.Date && day["Date"]?.Type != JTokenType.String ||
            day["AppUsageSeconds"] is not JObject))
            throw new JsonException("History must be an array of complete daily records.");
        var stats = json.ToObject<List<DailyStat>>()
            ?? throw new JsonException("History must be a JSON array.");
        if (stats.Any(x => x is null || x.Date == default || x.AppUsageSeconds is null ||
            !IsValidDuration(x.TotalFocusedSeconds) || !IsValidDuration(x.TotalRelaxedSeconds) ||
            !IsValidDuration(x.TotalBreakSeconds) || x.AppUsageSeconds.Values.Any(v => !IsValidDuration(v))))
            throw new JsonException("History contains an invalid day.");
        return stats;
    }

    private static bool IsValidDuration(double seconds) => double.IsFinite(seconds) && seconds >= 0;

    public void SaveOrUpdateToday(DailyStat currentStat)
    {
        lock (_gate)
        {
            if (!CanWrite) throw new InvalidDataException(RecoveryNotice);
            var allData = LoadAllData();
            if (!CanWrite) throw new InvalidDataException(RecoveryNotice);
            allData.RemoveAll(d => d.Date.Date == currentStat.Date.Date);
            allData.Add(currentStat.Copy());
            allData.Sort((a, b) => a.Date.CompareTo(b.Date));
            WriteAtomically(_filePath, JsonConvert.SerializeObject(allData, Formatting.Indented));
        }
    }

    public void ExportAllData(string destinationPath)
    {
        lock (_gate)
        {
            var data = LoadAllData();
            if (!CanWrite) throw new InvalidDataException(RecoveryNotice);
            WriteAtomically(destinationPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }

    public void DeleteAllData()
    {
        lock (_gate)
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
            if (File.Exists(_filePath + ".bak")) File.Delete(_filePath + ".bak");
            CanWrite = true;
            RecoveryNotice = null;
        }
    }

    public AppSettings LoadSettings()
    {
        lock (_gate)
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            try
            {
                var settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_settingsPath));
                return settings is { EyeRestIntervalMinutes: >= 5 and <= 120 } ? settings : new AppSettings();
            }
            catch (JsonException) { return new AppSettings(); }
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        if (settings.EyeRestIntervalMinutes is < 5 or > 120)
            throw new ArgumentOutOfRangeException(nameof(settings), "Reminder interval must be between 5 and 120 minutes.");
        lock (_gate) WriteAtomically(_settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
    }

    private static void WriteAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
