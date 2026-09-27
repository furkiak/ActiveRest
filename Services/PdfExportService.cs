using System;
using System.IO;
using System.Linq;
using ActiveRest.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace ActiveRest.Services
{
    public class PdfExportService
    {
        public void ExportReport(MainWindowViewModel vm, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White); 
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));   
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("ActiveRest activity summary").FontSize(24).SemiBold().FontColor("#1D1D1F");
                            col.Item().Text($"{vm.CurrentModeText} · generated {DateTime.Now:dd MMMM yyyy}").FontSize(12).FontColor("#52657A");
                        });
                    }); 
                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(20); 
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Padding(5).Border(1).BorderColor("#E5E5EA").Background("#F5F5F7").Padding(10).Column(c => {
                                c.Item().Text("Focused Work").FontSize(10).FontColor("#52657A");
                                c.Item().Text(vm.FocusStatText).FontSize(14).Bold().FontColor("#2C2C2E");
                            });
                            row.RelativeItem().Padding(5).Border(1).BorderColor("#E5E5EA").Background("#F5F5F7").Padding(10).Column(c => {
                                c.Item().Text("Audio active while idle").FontSize(10).FontColor("#52657A");
                                c.Item().Text(vm.RelaxStatText).FontSize(14).Bold().FontColor("#2C2C2E");
                            });
                            row.RelativeItem().Padding(5).Border(1).BorderColor("#E5E5EA").Background("#F5F5F7").Padding(10).Column(c => {
                                c.Item().Text("Rest").FontSize(10).FontColor("#52657A");
                                c.Item().Text(vm.BreakStatText).FontSize(14).Bold().FontColor("#2C2C2E");
                            });
                        });
                         
                        column.Item().Text("Detailed Report").FontSize(16).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(80);
                            });
                             
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Application Name");
                                header.Cell().Element(CellStyle).Text("Duration");
                                header.Cell().Element(CellStyle).Text("Ratio");

                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });
                             
                            foreach (var item in vm.AppUsageList)
                            {
                                table.Cell().Element(RowStyle).Text(item.AppName);
                                table.Cell().Element(RowStyle).Text(item.DurationText);
                                table.Cell().Element(RowStyle).Text(item.PercentageText);

                                static IContainer RowStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor("#E5E5EA");
                            }
                        });
                    });
                     
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Observed local activity, not a productivity score. | ").FontColor("#52657A");
                        x.CurrentPageNumber().FontColor("#1D1D1F");
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}
