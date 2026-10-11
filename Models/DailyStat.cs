using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ActiveRest.Models
{
    public class DailyStat
    {
        public const int HoursPerDay = 24;

        public DateTime Date { get; set; }

        /// <summary>First time the tracker saw activity on this day (kept under its original JSON name).</summary>
        [JsonProperty("FirstBootTime")]
        public DateTime FirstActiveTime { get; set; }

        public double TotalFocusedSeconds { get; set; }
        public double TotalRelaxedSeconds { get; set; }
        public double TotalBreakSeconds { get; set; }
        public Dictionary<string, double> AppUsageSeconds { get; set; } = new();

        /// <summary>Focused seconds per clock hour (index 0-23). Absent in data recorded by older versions.</summary>
        public double[] HourlyFocusSeconds { get; set; } = new double[HoursPerDay];

        /// <summary>Longest uninterrupted focus run of the day, in seconds.</summary>
        public double LongestFocusSeconds { get; set; }

        [JsonIgnore]
        public double TotalSeconds => TotalFocusedSeconds + TotalRelaxedSeconds + TotalBreakSeconds;

        public DailyStat() : this(DateTime.Today) { }

        public DailyStat(DateTime date)
        {
            Date = date.Date;
            FirstActiveTime = DateTime.Now;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            Date = Date.Date;
            AppUsageSeconds ??= new Dictionary<string, double>();
            if (HourlyFocusSeconds is not { Length: HoursPerDay })
            {
                var fixedSize = new double[HoursPerDay];
                if (HourlyFocusSeconds != null)
                    Array.Copy(HourlyFocusSeconds, fixedSize, Math.Min(HourlyFocusSeconds.Length, HoursPerDay));
                HourlyFocusSeconds = fixedSize;
            }
        }

        public DailyStat Clone() => new(Date)
        {
            FirstActiveTime = FirstActiveTime,
            TotalFocusedSeconds = TotalFocusedSeconds,
            TotalRelaxedSeconds = TotalRelaxedSeconds,
            TotalBreakSeconds = TotalBreakSeconds,
            AppUsageSeconds = new Dictionary<string, double>(AppUsageSeconds),
            HourlyFocusSeconds = (double[])HourlyFocusSeconds.Clone(),
            LongestFocusSeconds = LongestFocusSeconds
        };

        /// <summary>Adds another record of the same day (used to repair duplicated entries in a damaged file).</summary>
        public void MergeFrom(DailyStat other)
        {
            if (other.FirstActiveTime < FirstActiveTime) FirstActiveTime = other.FirstActiveTime;
            TotalFocusedSeconds += other.TotalFocusedSeconds;
            TotalRelaxedSeconds += other.TotalRelaxedSeconds;
            TotalBreakSeconds += other.TotalBreakSeconds;
            LongestFocusSeconds = Math.Max(LongestFocusSeconds, other.LongestFocusSeconds);
            foreach (var (app, seconds) in other.AppUsageSeconds)
                AppUsageSeconds[app] = AppUsageSeconds.GetValueOrDefault(app) + seconds;
            for (int i = 0; i < HoursPerDay; i++)
                HourlyFocusSeconds[i] += other.HourlyFocusSeconds[i];
        }

        public bool IsEmpty => TotalSeconds <= 0 && AppUsageSeconds.Count == 0 && !HourlyFocusSeconds.Any(h => h > 0);
    }
}
