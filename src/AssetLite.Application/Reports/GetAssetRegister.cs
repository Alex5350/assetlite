using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using ErrorOr;

namespace AssetLite.Application.Reports;

/// <summary>Returns the full asset register for export (Excel/PDF), ordered by tag.</summary>
public sealed record GetAssetRegisterQuery : IQuery<IReadOnlyList<AssetRegisterRowDto>>;

/// <summary>Handles <see cref="GetAssetRegisterQuery"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
public sealed class GetAssetRegisterHandler(
    IAssetRepository AssetRepository,
    IOfficeRepository OfficeRepository,
    ICategoryRepository CategoryRepository) : IQueryHandler<GetAssetRegisterQuery, IReadOnlyList<AssetRegisterRowDto>>
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<AssetRegisterRowDto>>> HandleAsync(GetAssetRegisterQuery query, CancellationToken cancellationToken = default)
    {
        var assets = (await AssetRepository.SearchAsync(
            new AssetSearchFilter(Page: 1, PageSize: int.MaxValue),
            cancellationToken)).Items;

        var officeNames = (await OfficeRepository.ListAllAsync(cancellationToken))
            .ToDictionary(office => office.Id, office => office.Name);
        var categoryNames = (await CategoryRepository.ListAsync(cancellationToken))
            .ToDictionary(category => category.Id, category => category.Name);

        return assets
            .OrderBy(asset => asset.Tag.Number)
            .Select(asset => new AssetRegisterRowDto(
                asset.Tag.Value,
                asset.Name,
                categoryNames.GetValueOrDefault(asset.CategoryId) ?? string.Empty,
                officeNames.GetValueOrDefault(asset.OfficeId) ?? string.Empty,
                asset.Status,
                asset.Condition,
                asset.Manufacturer,
                asset.Model,
                asset.SerialNumber,
                asset.PurchaseDate,
                asset.PurchaseCost?.Amount,
                asset.PurchaseCost?.Currency,
                asset.OpenAssignment?.AssigneeName,
                asset.OpenAssignment?.AssigneeEmail,
                asset.Notes))
            .ToList();
    }
}
