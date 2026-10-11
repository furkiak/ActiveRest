using ActiveRest.Localization;
using ActiveRest.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace ActiveRest.Services
{
    /// <summary>Renders a <see cref="ReportData"/> as a clean, light-themed A4 PDF.</summary>
    public sealed class PdfExportService
    {
        private const string Ink = "#1D1D1F", Muted = "#86868B", Hairline = "#E5E5EA", Surface = "#F5F5F7";
        private const string FocusColor = "#2DB84D", RelaxedColor = "#0A84FF", RestColor = "#FF9F0A";

        static PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void Export(ReportData data, string path, Loc loc)
        {
            var summary = data.Summary;
            var breakdown = ReportBuilder.Breakdown(data, loc);

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ActiveRest").FontSize(10).SemiBold().FontColor(RelaxedColor);
                                c.Item().Text(loc.Get("report.title")).FontSize(26).SemiBold();
                                c.Item().PaddingTop(2).Text(data.PeriodTitle).FontSize(13).FontColor(Muted);
                            });
                            row.ConstantItem(170).AlignRight().AlignBottom()
                                .Text(loc.Format("report.generated", data.GeneratedAt.ToString("g", loc.Culture))).FontSize(9).FontColor(Muted);
                        });
                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor(Hairline);
                    });

                    page.Content().PaddingVertical(18).Column(column =>
                    {
                        column.Spacing(18);

                        column.Item().Row(row =>
                        {
                            row.Spacing(10);
                            StatCard(row.RelativeItem(), loc.Get("report.focus"), summary.FocusSeconds, summary.FocusRatio, FocusColor, loc);
                            StatCard(row.RelativeItem(), loc.Get("report.relaxed"), summary.RelaxedSeconds, summary.RelaxedRatio, RelaxedColor, loc);
                            StatCard(row.RelativeItem(), loc.Get("report.rest"), summary.RestSeconds, summary.RestRatio, RestColor, loc);
                        });

                        if (summary.TotalSeconds > 0)
                        {
                            column.Item().Height(10).Row(row =>
                            {
                                if (summary.FocusRatio > 0) row.RelativeItem((float)summary.FocusRatio).Background(FocusColor);
                                if (summary.RelaxedRatio > 0) row.RelativeItem((float)summary.RelaxedRatio).Background(RelaxedColor);
                                if (summary.RestRatio > 0) row.RelativeItem((float)summary.RestRatio).Background(RestColor);
                            });
                        }

                        if (breakdown.Count > 0)
                        {
                            column.Item().Column(c =>
                            {
                                c.Item().PaddingBottom(6).Text(loc.Get("report.daily")).FontSize(14).SemiBold();
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                    });
                                    table.Header(h =>
                                    {
                                        h.Cell().Element(HeadCell).Text(loc.Get("report.date"));
                                        h.Cell().Element(HeadCell).Text(loc.Get("report.focus"));
                                        h.Cell().Element(HeadCell).Text(loc.Get("report.relaxed"));
                                        h.Cell().Element(HeadCell).Text(loc.Get("report.rest"));
                                    });
                                    foreach (var row in breakdown)
                                    {
                                        table.Cell().Element(BodyCell).Text(row.Label);
                                        table.Cell().Element(BodyCell).Text(loc.FormatDuration(row.Focus));
                                        table.Cell().Element(BodyCell).Text(loc.FormatDuration(row.Relaxed));
                                        table.Cell().Element(BodyCell).Text(loc.FormatDuration(row.Rest));
                                    }
                                });
                            });
                        }

                        column.Item().Column(c =>
                        {
                            c.Item().PaddingBottom(6).Text(loc.Get("report.apps")).FontSize(14).SemiBold();
                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(3);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Element(HeadCell).Text(loc.Get("report.application"));
                                    h.Cell().Element(HeadCell).Text(loc.Get("report.duration"));
                                    h.Cell().Element(HeadCell).Text(loc.Get("report.share"));
                                    h.Cell().Element(HeadCell).Text(string.Empty);
                                });

                                double total = summary.AppSecondsTotal;
                                foreach (var app in summary.Apps)
                                {
                                    double share = total > 0 ? app.Seconds / total : 0;
                                    table.Cell().Element(BodyCell).Text(AppNameFormatter.Pretty(app.App));
                                    table.Cell().Element(BodyCell).Text(loc.FormatDuration(app.Seconds));
                                    table.Cell().Element(BodyCell).Text(loc.FormatPercent(share));
                                    table.Cell().Element(BodyCell).AlignMiddle().Height(6).Row(r =>
                                    {
                                        if (share > 0.001) r.RelativeItem((float)share).Background(RelaxedColor);
                                        if (share < 0.999) r.RelativeItem((float)(1 - share)).Background(Surface);
                                    });
                                }
                            });
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(loc.Get("report.footer")).FontSize(9).FontColor(Muted);
                        row.AutoItem().Text(x =>
                        {
                            x.DefaultTextStyle(s => s.FontSize(9).FontColor(Muted));
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf(path);

            static IContainer HeadCell(IContainer c) =>
                c.DefaultTextStyle(x => x.SemiBold().FontSize(9).FontColor(Muted)).PaddingVertical(6).BorderBottom(1).BorderColor(Ink);

            static IContainer BodyCell(IContainer c) =>
                c.PaddingVertical(6).BorderBottom(1).BorderColor(Hairline);
        }

        private static void StatCard(IContainer container, string label, double seconds, double ratio, string color, Loc loc)
        {
            container.Background(Surface).Padding(12).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.ConstantItem(8).AlignMiddle().Height(8).Background(color);
                    r.RelativeItem().PaddingLeft(6).Text(label).FontSize(10).FontColor(Muted);
                });
                c.Item().PaddingTop(6).Text(loc.FormatDuration(seconds)).FontSize(18).SemiBold();
                c.Item().Text(loc.FormatPercent(ratio)).FontSize(10).FontColor(Muted);
            });
        }
    }
}
