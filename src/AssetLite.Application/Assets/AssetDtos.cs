using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;

namespace AssetLite.Application.Assets;

/// <summary>Detailed view of an asset including its full assignment history.</summary>
/// <param name="Id">Asset id.</param>
/// <param name="Tag">Canonical tag, e.g. AST-000123.</param>
/// <param name="Name">Display name.</param>
/// <param name="Manufacturer">Optional manufacturer.</param>
/// <param name="Model">Optional model.</param>
/// <param name="SerialNumber">Optional serial number.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Condition">Physical condition.</param>
/// <param name="PurchaseDate">Optional purchase date.</param>
/// <param name="PurchaseCostAmount">Optional purchase cost amount (2 dp).</param>
/// <param name="PurchaseCostCurrency">Purchase cost currency (e.g. USD) when a cost is set.</param>
/// <param name="Notes">Optional free-form notes.</param>
/// <param name="OfficeId">Office currently holding the asset.</param>
/// <param name="OfficeName">Name of that office (null when unknown).</param>
/// <param name="CategoryId">Asset category.</param>
/// <param name="CategoryName">Name of that category (null when unknown).</param>
/// <param name="CreatedAtUtc">UTC creation moment.</param>
/// <param name="CurrentAssigneeName">Open assignee name, or null when unassigned.</param>
/// <param name="CurrentAssigneeEmail">Open assignee email, or null when unassigned.</param>
/// <param name="Assignments">Assignment history, newest first.</param>
public sealed record AssetDetailDto(
    AssetId Id,
    string Tag,
    string Name,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    AssetStatus Status,
    AssetCondition Condition,
    DateOnly? PurchaseDate,
    decimal? PurchaseCostAmount,
    string? PurchaseCostCurrency,
    string? Notes,
    OfficeId OfficeId,
    string? OfficeName,
    CategoryId CategoryId,
    string? CategoryName,
    DateTimeOffset CreatedAtUtc,
    string? CurrentAssigneeName,
    string? CurrentAssigneeEmail,
    IReadOnlyList<AssignmentDto> Assignments);

/// <summary>A single assignment history entry.</summary>
/// <param name="Id">Assignment id.</param>
/// <param name="AssigneeName">Assignee display name.</param>
/// <param name="AssigneeEmail">Assignee email.</param>
/// <param name="AssignedAtUtc">UTC hand-over moment.</param>
/// <param name="ReturnedAtUtc">UTC return moment, or null while the assignment is open.</param>
public sealed record AssignmentDto(
    AssignmentId Id,
    string AssigneeName,
    string AssigneeEmail,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? ReturnedAtUtc);

/// <summary>Compact asset row for lists and search results.</summary>
/// <param name="Id">Asset id.</param>
/// <param name="Tag">Canonical tag.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Condition">Physical condition.</param>
/// <param name="OfficeId">Office currently holding the asset.</param>
/// <param name="OfficeName">Name of that office (null when unknown).</param>
/// <param name="CategoryId">Asset category.</param>
/// <param name="CategoryName">Name of that category (null when unknown).</param>
/// <param name="CurrentAssigneeName">Open assignee name, or null when unassigned.</param>
public sealed record AssetListItemDto(
    AssetId Id,
    string Tag,
    string Name,
    AssetStatus Status,
    AssetCondition Condition,
    OfficeId OfficeId,
    string? OfficeName,
    CategoryId CategoryId,
    string? CategoryName,
    string? CurrentAssigneeName);
