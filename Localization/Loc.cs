using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ActiveRest.Localization
{
    /// <summary>
    /// Runtime-switchable string table (English / Turkish). XAML binds through the indexer
    /// (see <see cref="TExtension"/>), so changing the language updates every open window instantly.
    /// </summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new();

        private IReadOnlyDictionary<string, string> _table = Strings.English;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after the language changed so view models can rebuild text they computed themselves.</summary>
        public event Action? LanguageChanged;

        public string Language { get; private set; } = "en";
        public CultureInfo Culture { get; private set; } = new CultureInfo("en-US");

        public string this[string key] => Get(key);

        public string Get(string key)
        {
            if (_table.TryGetValue(key, out var value)) return value;
            if (Strings.English.TryGetValue(key, out var fallback)) return fallback;
            return key;
        }

        public string Format(string key, params object[] args) =>
            string.Format(Culture, Get(key), args);

        /// <summary>Applies "auto", "en" or "tr". "auto" follows the operating system UI language.</summary>
        public void SetLanguage(string setting)
        {
            string resolved = setting switch
            {
                "tr" => "tr",
                "en" => "en",
                _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en"
            };

            if (resolved == Language) return;

            Language = resolved;
            Culture = new CultureInfo(resolved == "tr" ? "tr-TR" : "en-US");
            _table = resolved == "tr" ? Strings.Turkish : Strings.English;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke();
        }

        // ----- shared formatting helpers -----

        public string FormatDuration(double totalSeconds, bool includeSeconds = false)
        {
            if (totalSeconds < 0 || double.IsNaN(totalSeconds)) totalSeconds = 0;
            long whole = (long)totalSeconds;
            long h = whole / 3600, m = whole % 3600 / 60, s = whole % 60;
            string hu = Get("unit.h"), mu = Get("unit.m"), su = Get("unit.s");

            if (includeSeconds) return $"{h:00}{hu} {m:00}{mu} {s:00}{su}";
            if (h > 0) return $"{h}{hu} {m:00}{mu}";
            if (m > 0) return $"{m}{mu}";
            return whole > 0 ? $"{s}{su}" : $"0{mu}";
        }

        /// <summary>Compact target such as "6h" or "6h 30m".</summary>
        public string FormatGoal(double totalSeconds)
        {
            long minutes = (long)Math.Round(totalSeconds / 60);
            long h = minutes / 60, m = minutes % 60;
            string hu = Get("unit.h"), mu = Get("unit.m");
            if (h == 0) return $"{m}{mu}";
            return m == 0 ? $"{h}{hu}" : $"{h}{hu} {m:00}{mu}";
        }

        public string FormatPercent(double fraction)
        {
            int pct = (int)Math.Round(Math.Clamp(fraction, 0, 1) * 100);
            return Language == "tr" ? $"%{pct}" : $"{pct}%";
        }
    }
}
