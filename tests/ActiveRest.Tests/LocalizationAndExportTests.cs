using ActiveRest.Localization;
using ActiveRest.Models;
using ActiveRest.Services;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ActiveRest.Tests
{
    public class LocalizationAndExportTests
    {
        [Fact]
        public void Turkish_and_english_define_the_same_keys()
        {
            var en = Strings.English.Keys.ToHashSet();
            var tr = Strings.Turkish.Keys.ToHashSet();

            Assert.Empty(en.Except(tr));
            Assert.Empty(tr.Except(en));
        }

        [Fact]
        public void Durations_and_percentages_follow_the_language()
        {
            var loc = Loc.Instance;
            loc.SetLanguage("en");
            Assert.Equal("2h 05m", loc.FormatDuration(2 * 3600 + 5 * 60 + 9));
            Assert.Equal("02h 05m 09s", loc.FormatDuration(2 * 3600 + 5 * 60 + 9, includeSeconds: true));
            Assert.Equal("45s", loc.FormatDuration(45));
            Assert.Equal("0m", loc.FormatDuration(0));
            Assert.Equal("62%", loc.FormatPercent(0.62));

            loc.SetLanguage("tr");
            Assert.Equal("2sa 05dk", loc.FormatDuration(2 * 3600 + 5 * 60 + 9));
            Assert.Equal("%62", loc.FormatPercent(0.62));
            Assert.Equal("Odak", loc["mode.focus"]);

            loc.SetLanguage("en");
        }

        [Fact]
        public void Duration_over_a_day_keeps_counting_hours()
        {
            Loc.Instance.SetLanguage("en");
            Assert.Equal("40h 12m", Loc.Instance.FormatDuration(40 * 3600 + 12 * 60));
        }

        private static ReportData SampleReport()
        {
            var day = new DailyStat(new DateTime(2026, 10, 5)) { TotalFocusedSeconds = 7200, TotalRelaxedSeconds = 600, TotalBreakSeconds = 900 };
            day.AppUsageSeconds["Code"] = 5000;
            day.AppUsageSeconds["=cmd|' /C calc'!A0"] = 2200;
            var days = new[] { day };
            var range = StatsAggregator.GetRange(PeriodKind.Week, day.Date, DayOfWeek.Monday);
            return ReportBuilder.Build(PeriodKind.Week, range, days, day.Date, 6 * 3600, "5 - 11 Oct 2026");
        }

        [Fact]
        public void Csv_export_neutralises_formula_injection_and_writes_a_bom()
        {
            using var tmp = new TempFolder();
            string path = Path.Combine(tmp.Path, "r.csv");
            Loc.Instance.SetLanguage("en");

            new CsvExportService().Export(SampleReport(), path, Loc.Instance);

            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
            string text = File.ReadAllText(path);
            Assert.Contains("2026-10-05,7200,600,900", text);
            Assert.DoesNotContain("\n=cmd", text);
            Assert.Contains("Code", text);
        }

        [Fact]
        public void Pdf_export_produces_a_valid_pdf_in_both_languages()
        {
            using var tmp = new TempFolder();
            var data = SampleReport();

            foreach (var lang in new[] { "en", "tr" })
            {
                Loc.Instance.SetLanguage(lang);
                string path = Path.Combine(tmp.Path, $"r-{lang}.pdf");
                new PdfExportService().Export(data, path, Loc.Instance);

                byte[] header = File.ReadAllBytes(path).Take(5).ToArray();
                Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(header));
            }
            Loc.Instance.SetLanguage("en");
        }

        [Fact]
        public void App_names_are_friendly_and_hues_are_stable()
        {
            Assert.Equal("Google Chrome", AppNameFormatter.Pretty("Chrome"));
            Assert.Equal("Unknowntool", AppNameFormatter.Pretty("Unknowntool"));
            Assert.Equal(AppNameFormatter.Hue("Code"), AppNameFormatter.Hue("code"));
            Assert.InRange(AppNameFormatter.Hue("Anything"), 0, 360);
        }
    }
}
