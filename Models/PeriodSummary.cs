using System;
using System.Collections.Generic;
using System.Linq;

namespace ActiveRest.Models
{
    public sealed record AppUsage(string App, double Seconds);

    /// <summary>Aggregated numbers for a day, week, month or year.</summary>
    public sealed class PeriodSummary
    {
        public static readonly PeriodSummary Empty = new();

        public double FocusSeconds { get; init; }
        public double RelaxedSeconds { get; init; }
        public double RestSeconds { get; init; }
        public IReadOnlyList<AppUsage> Apps { get; init; } = Array.Empty<AppUsage>();
        public double LongestFocusSeconds { get; init; }
        public int ActiveDays { get; init; }

        /// <summary>Clock hour (0-23) with the most focus, or null when no hourly data exists.</summary>
        public int? PeakHour { get; init; }

        /// <summary>Daily goal multiplied by the number of days of the period that have already started.</summary>
        public double GoalSeconds { get; init; }

        public double TotalSeconds => FocusSeconds + RelaxedSeconds + RestSeconds;
        public double FocusRatio => TotalSeconds > 0 ? FocusSeconds / TotalSeconds : 0;
        public double RelaxedRatio => TotalSeconds > 0 ? RelaxedSeconds / TotalSeconds : 0;
        public double RestRatio => TotalSeconds > 0 ? RestSeconds / TotalSeconds : 0;
        public double GoalProgress => GoalSeconds > 0 ? Math.Min(1, FocusSeconds / GoalSeconds) : 0;
        public double AverageFocusPerActiveDay => ActiveDays > 0 ? FocusSeconds / ActiveDays : 0;
        public double AppSecondsTotal => Apps.Sum(a => a.Seconds);
    }

    /// <summary>Everything the exporters need to write a report; independent from any view model.</summary>
    public sealed class ReportData
    {
        public required string PeriodTitle { get; init; }
        public required PeriodKind Kind { get; init; }
        public required DateTime RangeStart { get; init; }
        public required DateTime RangeEndInclusive { get; init; }
        public required PeriodSummary Summary { get; init; }
        public required IReadOnlyList<DailyStat> Days { get; init; }
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
    }
}
