using ActiveRest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveRest.Services
{
    /// <summary>
    /// File-backed store of daily statistics. The whole history is cached in memory after the first read,
    /// writes are atomic (temp file + replace) and the previous good file is kept as a backup, so a crash
    /// or a power cut can never wipe the history.
    /// </summary>
    public sealed class DataManager
    {
        private readonly object _gate = new();
        private readonly string _filePath;
        private readonly string _backupPath;
        private Dictionary<DateTime, DailyStat>? _cache;
        private bool _dirty;
        private int _flushRunning;

        public string DataFolder { get; }

        public DataManager(string? dataFolder = null)
        {
            DataFolder = dataFolder ?? AppPaths.DataFolder;
            _filePath = Path.Combine(DataFolder, "stats.json");
            _backupPath = _filePath + ".bak";
            try { Directory.CreateDirectory(DataFolder); }
            catch (Exception ex) { Logger.Error("Cannot create data folder", ex); }
        }

        /// <summary>All stored days ordered by date. Returned records are shared: treat them as read-only.</summary>
        public IReadOnlyList<DailyStat> GetAll()
        {
            lock (_gate)
                return EnsureLoaded().Values.OrderBy(d => d.Date).ToList();
        }

        public DailyStat? GetDay(DateTime date)
        {
            lock (_gate)
                return EnsureLoaded().GetValueOrDefault(date.Date);
        }

        /// <summary>Days with startInclusive &lt;= date &lt; endExclusive, ordered by date.</summary>
        public IReadOnlyList<DailyStat> GetRange(DateTime startInclusive, DateTime endExclusive)
        {
            lock (_gate)
                return EnsureLoaded().Values
                    .Where(d => d.Date >= startInclusive.Date && d.Date < endExclusive.Date)
                    .OrderBy(d => d.Date)
                    .ToList();
        }

        /// <summary>Stores a private copy of the record in the cache. Call <see cref="Flush"/> to persist.</summary>
        public void Upsert(DailyStat stat)
        {
            var copy = stat.Clone();
            lock (_gate)
            {
                EnsureLoaded()[copy.Date] = copy;
                _dirty = true;
            }
        }

        /// <summary>Writes pending changes to disk synchronously.</summary>
        public void Flush()
        {
            string json;
            lock (_gate)
            {
                if (!_dirty || _cache == null) return;
                json = JsonConvert.SerializeObject(_cache.Values.OrderBy(d => d.Date).ToList(), Formatting.Indented);
                _dirty = false;
            }

            try
            {
                AtomicFile.Write(_filePath, json, _backupPath);
            }
            catch (Exception ex)
            {
                lock (_gate) _dirty = true;
                Logger.Error("Saving statistics failed", ex);
            }
        }

        /// <summary>Writes pending changes on a worker thread; overlapping requests are coalesced.</summary>
        public Task FlushAsync()
        {
            if (Interlocked.CompareExchange(ref _flushRunning, 1, 0) != 0) return Task.CompletedTask;
            return Task.Run(() =>
            {
                try { Flush(); }
                finally { Volatile.Write(ref _flushRunning, 0); }
            });
        }

        private Dictionary<DateTime, DailyStat> EnsureLoaded() => _cache ??= LoadFromDisk();

        private Dictionary<DateTime, DailyStat> LoadFromDisk()
        {
            List<DailyStat>? list = TryRead(_filePath);
            if (list == null && File.Exists(_filePath))
            {
                QuarantineCorruptFile();
                list = TryRead(_backupPath);
                if (list != null) Logger.Warn("stats.json was unreadable; restored from the backup copy.");
            }
            else if (list == null)
            {
                list = TryRead(_backupPath) ?? new List<DailyStat>();
            }

            var result = new Dictionary<DateTime, DailyStat>();
            foreach (var day in list ?? new List<DailyStat>())
            {
                if (result.TryGetValue(day.Date, out var existing)) existing.MergeFrom(day);
                else result[day.Date] = day;
            }
            return result;
        }

        private static List<DailyStat>? TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var list = JsonConvert.DeserializeObject<List<DailyStat>>(File.ReadAllText(path));
                return list?.Where(d => d != null).ToList();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Cannot read {Path.GetFileName(path)}", ex);
                return null;
            }
        }

        private void QuarantineCorruptFile()
        {
            try
            {
                string target = Path.Combine(DataFolder, $"stats.corrupt-{DateTime.Now:yyyyMMddHHmmss}.json");
                File.Move(_filePath, target);
                Logger.Warn($"Unreadable stats file moved to {Path.GetFileName(target)}");
            }
            catch (Exception ex)
            {
                Logger.Error("Could not quarantine the corrupt stats file", ex);
            }
        }
    }

    internal static class AtomicFile
    {
        /// <summary>Writes through a temp file so readers never observe a half-written file.</summary>
        public static void Write(string path, string content, string? backupPath = null)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, content);
            if (backupPath != null && File.Exists(path))
            {
                try { File.Copy(path, backupPath, overwrite: true); }
                catch (Exception ex) { Logger.Warn("Backup copy failed", ex); }
            }
            File.Move(temp, path, overwrite: true);
        }
    }
}
