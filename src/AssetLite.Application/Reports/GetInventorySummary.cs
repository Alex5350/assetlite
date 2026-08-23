using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Enums;
using ErrorOr;

namespace AssetLite.Application.Reports;

/// <summary>Produces the inventory summary (per-office and per-category status counts and values).</summary>
public sealed record GetInventorySummaryQuery : IQuery<InventorySummaryDto>;

/// <summary>Handles <see cref="GetInventorySummaryQuery"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="DateTimeProvider">Time provider.</param>
public sealed class GetInventorySummaryHandler(
    IAssetRepository AssetRepository,
    IOfficeRepository OfficeRepository,
    ICategoryRepository CategoryRepository,
    IDateTimeProvider DateTimeProvider) : IQueryHandler<GetInventorySummaryQuery, InventorySummaryDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<InventorySummaryDto>> HandleAsync(GetInventorySummaryQuery query, CancellationToken cancellationToken = default)
    {
        // Full scan through the search port: reports need the complete dataset.
        var assets = (await AssetRepository.SearchAsync(
            new AssetSearchFilter(Page: 1, PageSize: int.MaxValue),
            cancellationToken)).Items;
        var offices = await OfficeRepository.ListAllAsync(cancellationToken);
        var categories = await CategoryRepository.ListAsync(cancellationToken);

        var officeSummaries = offices
            .OrderBy(office => office.Name)
            .Select(office => SummarizeOffice(office, [.. assets.Where(asset => asset.OfficeId == office.Id)]))
            .ToList();

        var categorySummaries = categories
            .OrderBy(category => category.Name)
            .Select(category => SummarizeCategory(category, [.. assets.Where(asset => asset.CategoryId == category.Id)]))
            .ToList();

        return new InventorySummaryDto(
            DateTimeProvider.UtcNow,
            assets.Count,
            assets.Sum(asset => asset.PurchaseCost?.Amount ?? 0m),
            officeSummaries,
            categorySummaries);
    }

    private static OfficeSummaryDto SummarizeOffice(Domain.Offices.Office office, IReadOnlyList<Asset> officeAssets) => new(
        office.Id,
        office.Name,
        office.Code,
        officeAssets.Count,
        Count(officeAssets, AssetStatus.InStock),
        Count(officeAssets, AssetStatus.Assigned),
        Count(officeAssets, AssetStatus.Maintenance),
        Count(officeAssets, AssetStatus.Retired),
        Count(officeAssets, AssetStatus.Disposed),
        Sum(officeAssets));

    private static CategorySummaryDto SummarizeCategory(Domain.Categories.AssetCategory category, IReadOnlyList<Asset> categoryAssets) => new(
        category.Id,
        category.Name,
        categoryAssets.Count,
        Count(categoryAssets, AssetStatus.InStock),
        Count(categoryAssets, AssetStatus.Assigned),
        Count(categoryAssets, AssetStatus.Maintenance),
        Count(categoryAssets, AssetStatus.Retired),
        Count(categoryAssets, AssetStatus.Disposed),
        Sum(categoryAssets));

    private static int Count(IReadOnlyList<Asset> assets, AssetStatus status) =>
        assets.Count(asset => asset.Status == status);

    private static decimal Sum(IReadOnlyList<Asset> assets) =>
        assets.Sum(asset => asset.PurchaseCost?.Amount ?? 0m);
}
