using AssetLite.Application.Reports;

namespace AssetLite.Infrastructure.Reports;

/// <summary>
/// Renders the asset register as downloadable documents. Input rows come from the Application
/// layer's <c>GetAssetRegister</c> query; this service owns formatting only.
/// </summary>
public interface IReportExportService
{
    /// <summary>Exports the asset register as an Excel workbook.</summary>
    /// <param name="rows">Register rows (ordered by tag by the query handler).</param>
    /// <returns>The .xlsx file bytes.</returns>
    byte[] ExportAssetRegisterExcel(IReadOnlyList<AssetRegisterRowDto> rows);

    /// <summary>Exports the asset register as a landscape PDF.</summary>
    /// <param name="rows">Register rows (ordered by tag by the query handler).</param>
    /// <param name="summaryTitle">Document title shown in the header (e.g. company or report name).</param>
    /// <param name="generatedAt">Generation timestamp shown in the header (from IDateTimeProvider).</param>
    /// <returns>The PDF file bytes.</returns>
    byte[] ExportAssetRegisterPdf(IReadOnlyList<AssetRegisterRowDto> rows, string summaryTitle, DateTimeOffset generatedAt);
}
