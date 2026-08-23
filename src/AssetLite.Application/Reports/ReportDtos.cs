using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;

namespace AssetLite.Application.Reports;

/// <summary>Inventory totals grouped by office and by category, plus grand totals.</summary>
/// <param name="GeneratedAtUtc">UTC moment the summary was produced.</param>
/// <param name="TotalAssets">Total number of registered assets.</param>
/// <param name="TotalPurchaseValue">Sum of all purchase cost amounts (single-currency assumption, see remarks).</param>
/// <param name="Offices">Per-office summaries (ordered by office name; includes offices with no assets).</param>
/// <param name="Categories">Per-category summaries (ordered by category name; includes categories with no assets).</param>
/// <remarks>
/// Purchase values are summed as raw decimal amounts; assets default to USD so mixed-currency
/// inventories would require per-currency grouping in a future iteration.
/// </remarks>
public sealed record InventorySummaryDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalAssets,
    decimal TotalPurchaseValue,
    IReadOnlyList<OfficeSummaryDto> Offices,
    IReadOnlyList<CategorySummaryDto> Categories);

/// <summary>Status counts and purchase value for a single office.</summary>
/// <param name="OfficeId">Office id.</param>
/// <param name="OfficeName">Office name.</param>
/// <param name="OfficeCode">Office code.</param>
/// <param name="TotalAssets">Assets located in the office.</param>
/// <param name="InStockCount">Assets in stock.</param>
/// <param name="AssignedCount">Assets currently assigned.</param>
/// <param name="MaintenanceCount">Assets under maintenance.</param>
/// <param name="RetiredCount">Retired assets.</param>
/// <param name="DisposedCount">Disposed assets.</param>
/// <param name="TotalPurchaseValue">Sum of purchase cost amounts for the office.</param>
public sealed record OfficeSummaryDto(
    OfficeId OfficeId,
    string OfficeName,
    string OfficeCode,
    int TotalAssets,
    int InStockCount,
    int AssignedCount,
    int MaintenanceCount,
    int RetiredCount,
    int DisposedCount,
    decimal TotalPurchaseValue);

/// <summary>Status counts and purchase value for a single category.</summary>
/// <param name="CategoryId">Category id.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="TotalAssets">Assets in the category.</param>
/// <param name="InStockCount">Assets in stock.</param>
/// <param name="AssignedCount">Assets currently assigned.</param>
/// <param name="MaintenanceCount">Assets under maintenance.</param>
/// <param name="RetiredCount">Retired assets.</param>
/// <param name="DisposedCount">Disposed assets.</param>
/// <param name="TotalPurchaseValue">Sum of purchase cost amounts for the category.</param>
public sealed record CategorySummaryDto(
    CategoryId CategoryId,
    string CategoryName,
    int TotalAssets,
    int InStockCount,
    int AssignedCount,
    int MaintenanceCount,
    int RetiredCount,
    int DisposedCount,
    decimal TotalPurchaseValue);

/// <summary>One row of the exportable asset register (Excel/PDF export input).</summary>
/// <param name="Tag">Canonical asset tag.</param>
/// <param name="Name">Display name.</param>
/// <param name="CategoryName">Category name (empty when unknown).</param>
/// <param name="OfficeName">Office name (empty when unknown).</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Condition">Physical condition.</param>
/// <param name="Manufacturer">Optional manufacturer.</param>
/// <param name="Model">Optional model.</param>
/// <param name="SerialNumber">Optional serial number.</param>
/// <param name="PurchaseDate">Optional purchase date.</param>
/// <param name="PurchaseCostAmount">Optional purchase cost amount.</param>
/// <param name="PurchaseCostCurrency">Purchase cost currency when set.</param>
/// <param name="CurrentAssigneeName">Open assignee name, or null.</param>
/// <param name="CurrentAssigneeEmail">Open assignee email, or null.</param>
/// <param name="Notes">Optional notes.</param>
public sealed record AssetRegisterRowDto(
    string Tag,
    string Name,
    string CategoryName,
    string OfficeName,
    AssetStatus Status,
    AssetCondition Condition,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchaseCostAmount,
    string? PurchaseCostCurrency,
    string? CurrentAssigneeName,
    string? CurrentAssigneeEmail,
    string? Notes);
