using ActiveRest.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ActiveRest.Services
{
    public readonly record struct DateRange(DateTime Start, DateTime EndExclusive)
    {
        public int Days => (int)(EndExclusive - Start).TotalDays;
        public DateTime EndInclusive => EndExclusive.AddDays(-1);
        public bool Contains(DateTime date) => date.Date >= Start && date.Date < EndExclusive;
    }

    /// <summary>Pure functions that turn stored daily records into the numbers and charts shown in the UI and reports.</summary>
    public static class StatsAggregator
    {
        public static DateRange GetRange(PeriodKind kind, DateTime anchor, DayOfWeek firstDayOfWeek)
        {
            DateTime d = anchor.Date;
            return kind switch
            {
                PeriodKind.Day => new DateRange(d, d.AddDays(1)),
                PeriodKind.Week => WeekRange(d, firstDayOfWeek),
                PeriodKind.Month => new DateRange(new DateTime(d.Year, d.Month, 1), new DateTime(d.Year, d.Month, 1).AddMonths(1)),
                _ => new DateRange(new DateTime(d.Year, 1, 1), new DateTime(d.Year + 1, 1, 1))
            };
        }

        private static DateRange WeekRange(DateTime d, DayOfWeek first)
        {
            DateTime start = StartOfWeek(d, first);
            return new DateRange(start, start.AddDays(7));
        }

        /// <summary>First day of the week containing <paramref name="date"/>; correct for every day of the week, Sunday included.</summary>
        public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
        {
            int diff = (7 + (int)date.DayOfWeek - (int)firstDayOfWeek) % 7;
            return date.Date.AddDays(-diff);
        }

        public static DateTime Shift(PeriodKind kind, DateTime anchor, int steps) => kind switch
        {
            PeriodKind.Day => anchor.AddDays(steps),
            PeriodKind.Week => anchor.AddDays(7 * steps),
            PeriodKind.Month => anchor.AddMonths(steps),
            _ => anchor.AddYears(steps)
        };

        public static PeriodSummary Summarize(IEnumerable<DailyStat> days, DateRange range, DateTime today, double dailyGoalSeconds)
        {
            double focus = 0, relaxed = 0, rest = 0, longest = 0;
            int activeDays = 0;
            var apps = new Dictionary<string, double>();
            var hours = new double[DailyStat.HoursPerDay];

            foreach (var day in days)
            {
                if (!range.Contains(day.Date)) continue;
                focus += day.TotalFocusedSeconds;
                relaxed += day.TotalRelaxedSeconds;
                rest += day.TotalBreakSeconds;
                longest = Math.Max(longest, day.LongestFocusSeconds);
                if (day.TotalSeconds > 0) activeDays++;

                foreach (var (app, seconds) in day.AppUsageSeconds)
                    apps[app] = apps.GetValueOrDefault(app) + seconds;
                for (int h = 0; h < hours.Length; h++) hours[h] += day.HourlyFocusSeconds[h];
            }

            int? peak = null;
            double peakValue = hours.Max();
            if (peakValue > 0) peak = Array.IndexOf(hours, peakValue);

            int elapsedDays = Math.Clamp((int)(Min(range.EndExclusive, today.Date.AddDays(1)) - range.Start).TotalDays, 0, range.Days);

            return new PeriodSummary
            {
                FocusSeconds = focus,
                RelaxedSeconds = relaxed,
                RestSeconds = rest,
                Apps = apps.Select(kv => new AppUsage(kv.Key, kv.Value)).OrderByDescending(a => a.Seconds).ThenBy(a => a.App, StringComparer.OrdinalIgnoreCase).ToList(),
                LongestFocusSeconds = longest,
                ActiveDays = activeDays,
                PeakHour = peak,
                GoalSeconds = dailyGoalSeconds * elapsedDays
            };
        }

        public static IReadOnlyList<ChartBar> BuildChart(PeriodKind kind, DateRange range, IReadOnlyList<DailyStat> days, DateTime now, double dailyGoalSeconds, CultureInfo culture)
        {
            var byDate = days.ToDictionary(d => d.Date);
            var bars = new List<ChartBar>();

            switch (kind)
            {
                case PeriodKind.Day:
                    var day = byDate.GetValueOrDefault(range.Start);
                    for (int h = 0; h < DailyStat.HoursPerDay; h++)
                        bars.Add(new ChartBar(h.ToString("00", culture), day?.HourlyFocusSeconds[h] ?? 0, h % 6 == 0,
                            range.Start == now.Date && h == now.Hour, false));
                    break;

                case PeriodKind.Week:
                    for (DateTime d = range.Start; d < range.EndExclusive; d = d.AddDays(1))
                    {
                        double s = byDate.GetValueOrDefault(d)?.TotalFocusedSeconds ?? 0;
                        bars.Add(new ChartBar(culture.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek), s, true, d == now.Date, s >= dailyGoalSeconds && s > 0));
                    }
                    break;

                case PeriodKind.Month:
                    for (DateTime d = range.Start; d < range.EndExclusive; d = d.AddDays(1))
                    {
                        double s = byDate.GetValueOrDefault(d)?.TotalFocusedSeconds ?? 0;
                        bars.Add(new ChartBar(d.Day.ToString(culture), s, d.Day == 1 || d.Day % 5 == 0, d == now.Date, s >= dailyGoalSeconds && s > 0));
                    }
                    break;

                default:
                    for (int m = 1; m <= 12; m++)
                    {
                        double s = days.Where(d => d.Date.Month == m).Sum(d => d.TotalFocusedSeconds);
                        bars.Add(new ChartBar(culture.DateTimeFormat.GetAbbreviatedMonthName(m), s, true,
                            range.Start.Year == now.Year && m == now.Month, false));
                    }
                    break;
            }
            return bars;
        }

        /// <summary>Consecutive days (ending today, or yesterday while today is still in progress) that reached the focus goal.</summary>
        public static int ComputeStreak(IEnumerable<DailyStat> days, DateTime today, double goalSeconds)
        {
            if (goalSeconds <= 0) return 0;
            var focusByDate = days.ToDictionary(d => d.Date, d => d.TotalFocusedSeconds);
            DateTime end = Met(focusByDate, today.Date, goalSeconds) ? today.Date : today.Date.AddDays(-1);
            return CountRun(focusByDate, end, goalSeconds);
        }

        /// <summary>Length of the run of goal-reaching days that ends exactly on <paramref name="end"/>.</summary>
        public static int RunEndingAt(IEnumerable<DailyStat> days, DateTime end, double goalSeconds)
        {
            if (goalSeconds <= 0) return 0;
            return CountRun(days.ToDictionary(d => d.Date, d => d.TotalFocusedSeconds), end.Date, goalSeconds);
        }

        private static bool Met(Dictionary<DateTime, double> focusByDate, DateTime day, double goal) =>
            focusByDate.TryGetValue(day, out var s) && s >= goal;

        private static int CountRun(Dictionary<DateTime, double> focusByDate, DateTime end, double goal)
        {
            int streak = 0;
            for (DateTime cursor = end; Met(focusByDate, cursor, goal); cursor = cursor.AddDays(-1))
                streak++;
            return streak;
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    }
}
