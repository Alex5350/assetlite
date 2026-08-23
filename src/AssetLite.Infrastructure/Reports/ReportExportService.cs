using AssetLite.Application.Reports;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AssetLite.Infrastructure.Reports;

/// <summary>
/// ClosedXML (Excel) and QuestPDF (PDF) implementation of <see cref="IReportExportService"/>.
/// Runs under the QuestPDF Community license (see static constructor).
/// </summary>
internal sealed class ReportExportService : IReportExportService
{
    private const string WorksheetName = "Asset Register";

    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public byte[] ExportAssetRegisterExcel(IReadOnlyList<AssetRegisterRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(WorksheetName);

        WriteExcelHeader(sheet);
        WriteExcelRows(sheet, rows);
        WriteExcelTotals(sheet, rows);
        FormatExcelSheet(sheet, rows.Count);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public byte[] ExportAssetRegisterPdf(IReadOnlyList<AssetRegisterRowDto> rows, string summaryTitle, DateTimeOffset generatedAt)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Text(summaryTitle).FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    row.RelativeItem().AlignRight().Column(column =>
                    {
                        column.Item().AlignRight().Text("Asset Register").SemiBold();
                        column.Item().AlignRight()
                            .Text($"Generated {generatedAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC")
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingTop(6).Table(table => WritePdfTable(table, rows));

                page.Footer().PaddingTop(8).AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontColor(Colors.Grey.Darken1);
                    text.Span(" of ").FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static void WriteExcelHeader(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "Tag", "Asset", "Category", "Office", "Status", "Condition", "Manufacturer", "Model",
            "Serial Number", "Assignee", "Purchased", "Cost", "Currency", "Notes",
        };

        for (var index = 0; index < headers.Length; index++)
        {
            var cell = sheet.Cell(1, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        }
    }

    private static void WriteExcelRows(IXLWorksheet sheet, IReadOnlyList<AssetRegisterRowDto> rows)
    {
        for (var row = 0; row < rows.Count; row++)
        {
            var source = rows[row];
            var target = row + 2; // header occupies row 1

            sheet.Cell(target, 1).Value = source.Tag;
            sheet.Cell(target, 2).Value = source.Name;
            sheet.Cell(target, 3).Value = source.CategoryName;
            sheet.Cell(target, 4).Value = source.OfficeName;
            sheet.Cell(target, 5).Value = source.Status.ToString();
            sheet.Cell(target, 6).Value = source.Condition.ToString();
            sheet.Cell(target, 7).Value = source.Manufacturer;
            sheet.Cell(target, 8).Value = source.Model;
            sheet.Cell(target, 9).Value = source.SerialNumber;
            sheet.Cell(target, 10).Value = source.CurrentAssigneeName;
            if (source.PurchaseDate is { } purchased)
            {
                sheet.Cell(target, 11).Value = purchased.ToString("yyyy-MM-dd");
            }

            if (source.PurchaseCostAmount is { } amount)
            {
                var costCell = sheet.Cell(target, 12);
                costCell.Value = amount;
                costCell.Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(target, 13).Value = source.PurchaseCostCurrency;
            }

            sheet.Cell(target, 14).Value = source.Notes;
        }
    }

    private static void WriteExcelTotals(IXLWorksheet sheet, IReadOnlyList<AssetRegisterRowDto> rows)
    {
        // Purchase values are raw amounts (single-currency assumption, see ReportDtos remarks).
        var purchaseValue = rows.Sum(row => row.PurchaseCostAmount ?? 0m);

        var totalsRow = rows.Count + 3;
        sheet.Cell(totalsRow, 2).Value = $"Totals ({rows.Count} assets)";
        sheet.Cell(totalsRow, 2).Style.Font.Bold = true;

        var totalsCell = sheet.Cell(totalsRow, 12);
        totalsCell.Value = purchaseValue;
        totalsCell.Style.Font.Bold = true;
        totalsCell.Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(totalsRow, 13).Value = "Purchase value";
        sheet.Cell(totalsRow, 13).Style.Font.Bold = true;
    }

    private static void FormatExcelSheet(IXLWorksheet sheet, int rowCount)
    {
        var lastRow = Math.Max(rowCount + 3, 1);
        var range = sheet.Range(1, 1, lastRow, 14);
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void WritePdfTable(TableDescriptor table, IReadOnlyList<AssetRegisterRowDto> rows)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(70); // Tag
            columns.RelativeColumn(3);  // Asset
            columns.RelativeColumn(2);  // Category
            columns.RelativeColumn(2);  // Office
            columns.ConstantColumn(60); // Status
            columns.RelativeColumn(2);  // Assignee
            columns.ConstantColumn(70); // Purchased
            columns.ConstantColumn(70); // Cost
        });

        table.Header(header =>
        {
            foreach (var caption in new[] { "Tag", "Asset", "Category", "Office", "Status", "Assignee", "Purchased", "Cost" })
            {
                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(caption).FontColor(Colors.White).SemiBold();
            }
        });

        var zebra = false;
        foreach (var row in rows)
        {
            var background = zebra ? Colors.Grey.Lighten4 : Colors.White;
            zebra = !zebra;

            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Tag).SemiBold();
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Name);
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.CategoryName);
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.OfficeName);
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Status.ToString());
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.CurrentAssigneeName ?? "—");
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                .Text(row.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—");
            table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                .Text(row.PurchaseCostAmount is { } amount ? $"{amount:N2} {row.PurchaseCostCurrency}" : "—");
        }
    }
}
