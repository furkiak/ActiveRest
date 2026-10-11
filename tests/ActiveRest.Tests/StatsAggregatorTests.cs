using ActiveRest.Models;
using ActiveRest.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace ActiveRest.Tests
{
    public class StatsAggregatorTests
    {
        private static DailyStat Day(DateTime date, double focus = 0, double relaxed = 0, double rest = 0) =>
            new(date) { TotalFocusedSeconds = focus, TotalRelaxedSeconds = relaxed, TotalBreakSeconds = rest };

        [Theory]
        [InlineData("2026-10-05", "2026-10-05")] // Monday
        [InlineData("2026-10-07", "2026-10-05")] // Wednesday
        [InlineData("2026-10-11", "2026-10-05")] // Sunday: the original code returned the *next* Monday
        public void Week_starts_on_monday_for_every_weekday(string anchor, string expectedStart)
        {
            var range = StatsAggregator.GetRange(PeriodKind.Week, DateTime.Parse(anchor, CultureInfo.InvariantCulture), DayOfWeek.Monday);

            Assert.Equal(DateTime.Parse(expectedStart, CultureInfo.InvariantCulture), range.Start);
            Assert.Equal(7, range.Days);
        }

        [Fact]
        public void Week_respects_sunday_as_first_day()
        {
            var range = StatsAggregator.GetRange(PeriodKind.Week, new DateTime(2026, 10, 11), DayOfWeek.Sunday);
            Assert.Equal(new DateTime(2026, 10, 11), range.Start);
        }

        [Fact]
        public void Month_and_year_ranges_cover_the_whole_period()
        {
            var month = StatsAggregator.GetRange(PeriodKind.Month, new DateTime(2028, 2, 14), DayOfWeek.Monday);
            Assert.Equal(new DateTime(2028, 2, 1), month.Start);
            Assert.Equal(29, month.Days); // leap year

            var year = StatsAggregator.GetRange(PeriodKind.Year, new DateTime(2026, 6, 1), DayOfWeek.Monday);
            Assert.Equal(new DateTime(2026, 1, 1), year.Start);
            Assert.Equal(365, year.Days);
        }

        [Fact]
        public void Shift_moves_by_one_period()
        {
            var d = new DateTime(2026, 1, 31);
            Assert.Equal(new DateTime(2026, 2, 1), StatsAggregator.Shift(PeriodKind.Day, d, 1));
            Assert.Equal(new DateTime(2026, 1, 24), StatsAggregator.Shift(PeriodKind.Week, d, -1));
            Assert.Equal(new DateTime(2026, 2, 28), StatsAggregator.Shift(PeriodKind.Month, d, 1));
            Assert.Equal(new DateTime(2025, 1, 31), StatsAggregator.Shift(PeriodKind.Year, d, -1));
        }

        [Fact]
        public void Summarize_adds_up_modes_and_ignores_days_outside_the_range()
        {
            var monday = new DateTime(2026, 10, 5);
            var days = new List<DailyStat>
            {
                Day(monday, 3600, 600, 300),
                Day(monday.AddDays(2), 7200, 0, 900),
                Day(monday.AddDays(-1), 9999, 9999, 9999), // previous week
            };
            days[0].AppUsageSeconds["Code"] = 3000;
            days[1].AppUsageSeconds["Code"] = 5000;
            days[1].AppUsageSeconds["Chrome"] = 2200;

            var range = StatsAggregator.GetRange(PeriodKind.Week, monday, DayOfWeek.Monday);
            var s = StatsAggregator.Summarize(days, range, new DateTime(2026, 10, 7), 6 * 3600);

            Assert.Equal(10800, s.FocusSeconds);
            Assert.Equal(600, s.RelaxedSeconds);
            Assert.Equal(1200, s.RestSeconds);
            Assert.Equal(2, s.ActiveDays);
            Assert.Equal("Code", s.Apps[0].App);
            Assert.Equal(8000, s.Apps[0].Seconds);
            // Monday..Wednesday have started, so the goal covers 3 days.
            Assert.Equal(3 * 6 * 3600, s.GoalSeconds);
        }

        [Fact]
        public void Goal_is_zero_for_future_periods_and_full_for_past_ones()
        {
            var today = new DateTime(2026, 10, 11);
            var future = StatsAggregator.GetRange(PeriodKind.Month, new DateTime(2026, 12, 1), DayOfWeek.Monday);
            var past = StatsAggregator.GetRange(PeriodKind.Month, new DateTime(2026, 9, 1), DayOfWeek.Monday);

            Assert.Equal(0, StatsAggregator.Summarize(Array.Empty<DailyStat>(), future, today, 100).GoalSeconds);
            Assert.Equal(30 * 100, StatsAggregator.Summarize(Array.Empty<DailyStat>(), past, today, 100).GoalSeconds);
        }

        [Fact]
        public void Peak_hour_comes_from_hourly_buckets()
        {
            var d = Day(new DateTime(2026, 10, 5), 5000);
            d.HourlyFocusSeconds[9] = 1000;
            d.HourlyFocusSeconds[14] = 3500;
            var s = StatsAggregator.Summarize(new[] { d }, StatsAggregator.GetRange(PeriodKind.Day, d.Date, DayOfWeek.Monday), d.Date, 100);

            Assert.Equal(14, s.PeakHour);
        }

        [Fact]
        public void Peak_hour_is_null_without_hourly_data()
        {
            var d = Day(new DateTime(2026, 10, 5), 5000); // legacy record
            var s = StatsAggregator.Summarize(new[] { d }, StatsAggregator.GetRange(PeriodKind.Day, d.Date, DayOfWeek.Monday), d.Date, 100);

            Assert.Null(s.PeakHour);
        }

        [Fact]
        public void Chart_has_one_bar_per_slot()
        {
            var now = new DateTime(2026, 10, 11, 15, 0, 0);
            var culture = CultureInfo.InvariantCulture;
            int Count(PeriodKind k) => StatsAggregator.BuildChart(k, StatsAggregator.GetRange(k, now, DayOfWeek.Monday), Array.Empty<DailyStat>(), now, 3600, culture).Count;

            Assert.Equal(24, Count(PeriodKind.Day));
            Assert.Equal(7, Count(PeriodKind.Week));
            Assert.Equal(31, Count(PeriodKind.Month));
            Assert.Equal(12, Count(PeriodKind.Year));
        }

        [Fact]
        public void Week_chart_marks_today_and_goal_days()
        {
            var monday = new DateTime(2026, 10, 5);
            var days = new[] { Day(monday, 7000), Day(monday.AddDays(1), 1000) };
            var now = monday.AddDays(1).AddHours(10);
            var bars = StatsAggregator.BuildChart(PeriodKind.Week, StatsAggregator.GetRange(PeriodKind.Week, now, DayOfWeek.Monday), days, now, 3600, CultureInfo.InvariantCulture);

            Assert.True(bars[0].MetGoal);
            Assert.False(bars[1].MetGoal);
            Assert.True(bars[1].IsCurrent);
            Assert.Equal(1, bars.Count(b => b.IsCurrent));
        }

        [Fact]
        public void Streak_counts_consecutive_goal_days_and_survives_an_unfinished_today()
        {
            var today = new DateTime(2026, 10, 11);
            var days = new[]
            {
                Day(today.AddDays(-4), 100),         // misses the goal -> breaks the run
                Day(today.AddDays(-3), 5000),
                Day(today.AddDays(-2), 5000),
                Day(today.AddDays(-1), 5000),
                Day(today, 10),                      // today not finished yet
            };

            Assert.Equal(3, StatsAggregator.ComputeStreak(days, today, 3600));

            days[4].TotalFocusedSeconds = 4000;
            Assert.Equal(4, StatsAggregator.ComputeStreak(days, today, 3600));
        }

        [Fact]
        public void Streak_is_zero_without_a_goal_or_data()
        {
            Assert.Equal(0, StatsAggregator.ComputeStreak(Array.Empty<DailyStat>(), DateTime.Today, 3600));
            Assert.Equal(0, StatsAggregator.ComputeStreak(new[] { Day(DateTime.Today, 9999) }, DateTime.Today, 0));
        }
    }
}
