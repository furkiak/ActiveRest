using ActiveRest.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ActiveRest.Services
{
    /// <summary>Single shared <see cref="AppSettings"/> instance. Edits are persisted and broadcast so every consumer reacts immediately.</summary>
    public sealed class SettingsStore
    {
        private readonly string _path;

        public AppSettings Current { get; }

        /// <summary>Raised after every successful <see cref="Update"/>.</summary>
        public event Action? Changed;

        public SettingsStore(string? dataFolder = null)
        {
            _path = Path.Combine(dataFolder ?? AppPaths.DataFolder, "settings.json");
            Current = Load();
        }

        public void Update(Action<AppSettings> edit)
        {
            edit(Current);
            Current.Normalize();
            Save();
            Changed?.Invoke();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var loaded = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_path));
                    if (loaded != null)
                    {
                        loaded.Normalize();
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("settings.json is unreadable; using defaults", ex);
            }
            return new AppSettings();
        }

        private void Save()
        {
            try
            {
                AtomicFile.Write(_path, JsonConvert.SerializeObject(Current, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.Error("Saving settings failed", ex);
            }
        }
    }
}
