using ActiveRest.Localization;
using ActiveRest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActiveRest.Services
{
    public sealed record BreakdownRow(string Label, double Focus, double Relaxed, double Rest);

    public static class ReportBuilder
    {
        public static ReportData Build(PeriodKind kind, DateRange range, IReadOnlyList<DailyStat> days, DateTime now, double dailyGoalSeconds, string periodTitle)
        {
            return new ReportData
            {
                PeriodTitle = periodTitle,
                Kind = kind,
                RangeStart = range.Start,
                RangeEndInclusive = range.EndInclusive,
                Summary = StatsAggregator.Summarize(days, range, now, dailyGoalSeconds),
                Days = days.Where(d => range.Contains(d.Date)).OrderBy(d => d.Date).ToList(),
                GeneratedAt = now
            };
        }

        /// <summary>Rows of the breakdown table: one per day, or one per month for yearly reports. Empty for a single day.</summary>
        public static IReadOnlyList<BreakdownRow> Breakdown(ReportData data, Loc loc)
        {
            var culture = loc.Culture;
            switch (data.Kind)
            {
                case PeriodKind.Day:
                    return Array.Empty<BreakdownRow>();

                case PeriodKind.Year:
                    return data.Days
                        .GroupBy(d => d.Date.Month)
                        .OrderBy(g => g.Key)
                        .Select(g => new BreakdownRow(culture.DateTimeFormat.GetMonthName(g.Key),
                            g.Sum(d => d.TotalFocusedSeconds), g.Sum(d => d.TotalRelaxedSeconds), g.Sum(d => d.TotalBreakSeconds)))
                        .ToList();

                default:
                    return data.Days
                        .Where(d => d.TotalSeconds > 0)
                        .Select(d => new BreakdownRow(d.Date.ToString("ddd, d MMM", culture),
                            d.TotalFocusedSeconds, d.TotalRelaxedSeconds, d.TotalBreakSeconds))
                        .ToList();
            }
        }
    }
}
