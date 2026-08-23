using AssetLite.Domain.Assets;

namespace AssetLite.Application.Assets;

/// <summary>
/// Maps <see cref="Asset"/> aggregates to application DTOs. Office and category names are
/// supplied by the caller (they are fetched through their own ports).
/// </summary>
internal static class AssetMappings
{
    /// <summary>Maps an asset to its detailed DTO.</summary>
    /// <param name="asset">The asset.</param>
    /// <param name="officeName">Name of the office holding the asset (or null).</param>
    /// <param name="categoryName">Name of the asset's category (or null).</param>
    /// <returns>The detailed DTO.</returns>
    public static AssetDetailDto ToDetailDto(this Asset asset, string? officeName, string? categoryName)
    {
        var current = asset.OpenAssignment;
        return new AssetDetailDto(
            asset.Id,
            asset.Tag.Value,
            asset.Name,
            asset.Manufacturer,
            asset.Model,
            asset.SerialNumber,
            asset.Status,
            asset.Condition,
            asset.PurchaseDate,
            asset.PurchaseCost?.Amount,
            asset.PurchaseCost?.Currency,
            asset.Notes,
            asset.OfficeId,
            officeName,
            asset.CategoryId,
            categoryName,
            asset.CreatedAtUtc,
            current?.AssigneeName,
            current?.AssigneeEmail,
            [.. asset.Assignments
                .OrderByDescending(assignment => assignment.AssignedAtUtc)
                .Select(assignment => new AssignmentDto(
                    assignment.Id,
                    assignment.AssigneeName,
                    assignment.AssigneeEmail,
                    assignment.AssignedAtUtc,
                    assignment.ReturnedAtUtc))]);
    }

    /// <summary>Maps an asset to its compact list DTO.</summary>
    /// <param name="asset">The asset.</param>
    /// <param name="officeName">Name of the office holding the asset (or null).</param>
    /// <param name="categoryName">Name of the asset's category (or null).</param>
    /// <returns>The list DTO.</returns>
    public static AssetListItemDto ToListItemDto(this Asset asset, string? officeName, string? categoryName) =>
        new(
            asset.Id,
            asset.Tag.Value,
            asset.Name,
            asset.Status,
            asset.Condition,
            asset.OfficeId,
            officeName,
            asset.CategoryId,
            categoryName,
            asset.OpenAssignment?.AssigneeName);
}
