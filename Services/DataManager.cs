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
    public StorageProfile Profile { get; }
    public string? RecoveryNotice { get; private set; }
    public string? SettingsRecoveryNotice { get; private set; }
    public bool CanWrite { get; private set; } = true;
    public bool CanWriteSettings { get; private set; } = true;

    public DataManager(string? dataFolder = null)
    {
        Profile = dataFolder == null ? AppStorage.Profile : AppStorage.SeparateProfile(dataFolder);
        DataFolder = Profile.Directory;
        AppStorage.EnsureNoLinks(DataFolder);
        Directory.CreateDirectory(DataFolder);
        _filePath = Path.Combine(DataFolder, "stats.json");
        _settingsPath = Path.Combine(DataFolder, "settings.json");
        EnsureFilesAreNotLinked();
    }

    public List<DailyStat> LoadAllData()
    {
        lock (_gate)
        {
            EnsureFilesAreNotLinked();
            if (!CanWrite) return new List<DailyStat>();
            if (!File.Exists(_filePath))
            {
                if (!File.Exists(_filePath + ".bak")) return new List<DailyStat>();
                try { return RestoreBackup(); }
                catch (UnsupportedDataException)
                {
                    return BlockHistory("The history backup uses an unsupported format. Tracking is off and the file is preserved for manual recovery.");
                }
                catch (JsonException)
                {
                    CanWrite = false;
                    RecoveryNotice = "The local history backup is damaged. Tracking is paused so the file is not overwritten.";
                    return new List<DailyStat>();
                }
            }

            try
            {
                var stats = ReadStats(_filePath);
                if (File.Exists(_filePath + ".bak"))
                {
                    try { ReadStats(_filePath + ".bak"); }
                    catch (Exception ex) when (ex is JsonException or UnsupportedDataException)
                    {
                        return BlockHistory("The history backup needs recovery. Tracking is off so neither copy is overwritten or deleted.");
                    }
                }
                return stats;
            }
            catch (UnsupportedDataException)
            {
                return BlockHistory("Local history uses an unsupported format. Tracking is off and all copies are preserved for manual recovery.");
            }
            catch (JsonException)
            {
                if (File.Exists(_filePath + ".bak"))
                {
                    try { return RestoreBackup(); }
                    catch (UnsupportedDataException) { /* Preserve the unreadable main and newer backup. */ }
                    catch (JsonException) { /* Both copies need manual recovery. */ }
                }

                CanWrite = false;
                RecoveryNotice = "The local history is damaged. Tracking is paused so the original file is not overwritten. Open the data folder to recover it.";
                return new List<DailyStat>();
            }
        }
    }

    private List<DailyStat> BlockHistory(string notice)
    {
        CanWrite = false;
        RecoveryNotice = notice;
        return new List<DailyStat>();
    }

    private void EnsureFilesAreNotLinked()
    {
        foreach (var file in new[] { _filePath, _filePath + ".bak", _settingsPath, _settingsPath + ".bak" })
            AppStorage.EnsureNoLinks(file);
    }

    private sealed class UnsupportedDataException(string message) : IOException(message);

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
        if (json is not JArray) throw new UnsupportedDataException("History format is unsupported.");
        var allowed = new[] { "Date", "FirstBootTime", "TotalFocusedSeconds", "TotalRelaxedSeconds", "TotalBreakSeconds", "AppUsageSeconds" };
        if (((JArray)json).OfType<JObject>().Any(day => day.Properties().Any(p => !allowed.Contains(p.Name))))
            throw new UnsupportedDataException("History contains unrecognized fields; preserve it for the matching application version.");
        if (json is not JArray array || array.Any(item => item is not JObject day ||
            day["Date"]?.Type != JTokenType.Date && day["Date"]?.Type != JTokenType.String ||
            day["FirstBootTime"] == null || day["TotalFocusedSeconds"] == null ||
            day["TotalRelaxedSeconds"] == null || day["TotalBreakSeconds"] == null ||
            day["AppUsageSeconds"] is not JObject))
            throw new JsonException("History must be an array of complete daily records.");
        var stats = json.ToObject<List<DailyStat>>()
            ?? throw new JsonException("History must be a JSON array.");
        if (stats.Any(x => x is null || x.Date == default || x.AppUsageSeconds is null ||
            x.FirstBootTime == default ||
            !IsValidDuration(x.TotalFocusedSeconds) || !IsValidDuration(x.TotalRelaxedSeconds) ||
            !IsValidDuration(x.TotalBreakSeconds) || x.AppUsageSeconds.Values.Any(v => !IsValidDuration(v))))
            throw new JsonException("History contains an invalid day.");
        if (stats.Select(x => x.Date.Date).Distinct().Count() != stats.Count)
            throw new UnsupportedDataException("History has duplicate days; preserve all rows for manual reconciliation.");
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
            LoadAllData();
            if (!CanWrite) throw new InvalidDataException("Unreadable history is preserved. Copy and repair the files from Open folder before deleting history.");
            if (File.Exists(_filePath + ".bak"))
            {
                try { ReadStats(_filePath + ".bak"); }
                catch (Exception ex) when (ex is JsonException or UnsupportedDataException)
                { throw new InvalidDataException("The backup needs manual recovery and was not deleted."); }
            }
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
            EnsureFilesAreNotLinked();
            if (!File.Exists(_settingsPath) && !File.Exists(_settingsPath + ".bak")) return new AppSettings();
            try
            {
                if (!File.Exists(_settingsPath)) throw new JsonException("Settings backup needs manual recovery.");
                var settings = ReadSettings(_settingsPath);
                if (File.Exists(_settingsPath + ".bak")) ReadSettings(_settingsPath + ".bak");
                return settings;
            }
            catch (JsonException)
            {
                CanWriteSettings = false;
                SettingsRecoveryNotice = "Settings need recovery. Safe defaults are shown, but the original file will not be overwritten. Copy and repair it from Open folder.";
                return new AppSettings();
            }
        }
    }

    private static AppSettings ReadSettings(string path)
    {
        var json = JToken.Parse(File.ReadAllText(path));
        var allowed = new[] { "EyeRestIntervalMinutes", "IsEyeRestEnabled", "WeekStartsOnSunday" };
        if (json is not JObject value || value.Properties().Any(p => !allowed.Contains(p.Name)) ||
            value["EyeRestIntervalMinutes"]?.Type != JTokenType.Integer ||
            value["IsEyeRestEnabled"]?.Type != JTokenType.Boolean ||
            value["WeekStartsOnSunday"] is { Type: not JTokenType.Boolean })
            throw new JsonException("Settings have an unsupported or incomplete format.");
        var settings = value.ToObject<AppSettings>();
        if (settings is not { EyeRestIntervalMinutes: >= 5 and <= 120 })
            throw new JsonException("Reminder settings are outside the supported range.");
        return settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        if (settings.EyeRestIntervalMinutes is < 5 or > 120)
            throw new ArgumentOutOfRangeException(nameof(settings), "Reminder interval must be between 5 and 120 minutes.");
        lock (_gate)
        {
            LoadSettings();
            if (!CanWriteSettings) throw new InvalidDataException(SettingsRecoveryNotice);
            WriteAtomically(_settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
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
