using AssetLite.Api.Dispatching;
using AssetLite.Application.Abstractions;
using AssetLite.Application.Reports;
using AssetLite.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AssetLite.Api.Controllers;

/// <summary>Inventory reporting and export endpoints.</summary>
/// <param name="dispatcher">Boundary dispatcher.</param>
/// <param name="exportService">Excel/PDF renderer for the asset register.</param>
/// <param name="dateTimeProvider">Provides the "generated at" timestamp for exports.</param>
[Route("api/reports")]
public sealed class ReportsController(
    RequestDispatcher dispatcher,
    IReportExportService exportService,
    IDateTimeProvider dateTimeProvider) : ApiControllerBase(dispatcher)
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Returns the inventory summary: totals plus per-office and per-category breakdowns.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(InventorySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetInventorySummaryQuery, InventorySummaryDto>(new(), cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Downloads the full asset register as an Excel workbook (asset-register.xlsx).</summary>
    [HttpGet("register/excel")]
    [Produces(ExcelContentType)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportRegisterExcel(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetAssetRegisterQuery, IReadOnlyList<AssetRegisterRowDto>>(new(), cancellationToken);
        return From(result, rows => File(exportService.ExportAssetRegisterExcel(rows), ExcelContentType, "asset-register.xlsx"));
    }

    /// <summary>Downloads the full asset register as a landscape PDF (asset-register.pdf).</summary>
    [HttpGet("register/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportRegisterPdf(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetAssetRegisterQuery, IReadOnlyList<AssetRegisterRowDto>>(new(), cancellationToken);
        return From(result, rows => File(
            exportService.ExportAssetRegisterPdf(rows, "AssetLite Asset Register", dateTimeProvider.UtcNow),
            "application/pdf",
            "asset-register.pdf"));
    }
}
