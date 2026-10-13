using ActiveRest.Localization;
using ActiveRest.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ActiveRest.Services
{
    /// <summary>Writes a spreadsheet-friendly CSV (UTF-8 with BOM, whole seconds, no locale-dependent decimals).</summary>
    public sealed class CsvExportService
    {
        public void Export(ReportData data, string path, Loc loc)
        {
            // Excel splits on the list separator of the user's regional settings: ';' for Turkish, ',' otherwise.
            char sep = loc.Language == "tr" ? ';' : ',';
            var sb = new StringBuilder();

            void Row(params string[] cells) => sb.AppendLine(string.Join(sep, cells.Select(c => Escape(c, sep))));

            Row(loc.Get("report.title"), data.PeriodTitle);
            Row(loc.Format("report.generated", data.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
            sb.AppendLine();

            Row(loc.Get("report.date"), loc.Get("report.focus") + " (" + loc.Get("csv.seconds") + ")",
                loc.Get("report.relaxed") + " (" + loc.Get("csv.seconds") + ")", loc.Get("report.rest") + " (" + loc.Get("csv.seconds") + ")");
            foreach (var day in data.Days.OrderBy(d => d.Date))
                Row(day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Seconds(day.TotalFocusedSeconds), Seconds(day.TotalRelaxedSeconds), Seconds(day.TotalBreakSeconds));
            Row(loc.Get("report.summary"), Seconds(data.Summary.FocusSeconds), Seconds(data.Summary.RelaxedSeconds), Seconds(data.Summary.RestSeconds));
            sb.AppendLine();

            Row(loc.Get("report.application"), loc.Get("csv.seconds"), loc.Get("report.share"));
            double total = data.Summary.AppSecondsTotal;
            foreach (var app in data.Summary.Apps)
                Row(AppNameFormatter.Pretty(app.App), Seconds(app.Seconds), total > 0 ? (app.Seconds / total).ToString("0.000", CultureInfo.InvariantCulture) : "0");

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string Seconds(double value) => ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

        internal static string Escape(string cell, char sep)
        {
            // Neutralise spreadsheet formula injection from process names (OWASP: = + - @ tab CR) and quote cells that need it.
            // Only text cells pass through here; numbers are formatted separately and are never negative.
            if (cell.Length > 0 && cell[0] is '=' or '+' or '-' or '@' or '\t' or '\r') cell = "'" + cell;
            if (cell.IndexOfAny(new[] { sep, '"', '\n', '\r' }) >= 0)
                cell = "\"" + cell.Replace("\"", "\"\"") + "\"";
            return cell;
        }
    }
}
