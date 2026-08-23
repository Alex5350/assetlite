using AssetLite.Application.Abstractions;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>
/// Paged asset search. When <paramref name="IncludeDescendantOffices"/> is set together with
/// <paramref name="OfficeId"/>, the office plus all its descendants are searched (resolved
/// through the office hierarchy domain service).
/// </summary>
/// <param name="OfficeId">Optional exact office filter.</param>
/// <param name="IncludeDescendantOffices">Search the office subtree instead of a single office.</param>
/// <param name="CategoryId">Optional category filter.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="SearchText">Optional contains-match over name, serial number, tag and model.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1-100).</param>
public sealed record SearchAssetsQuery(
    OfficeId? OfficeId = null,
    bool IncludeDescendantOffices = false,
    CategoryId? CategoryId = null,
    AssetStatus? Status = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AssetListItemDto>>;

/// <summary>FluentValidation validator for <see cref="SearchAssetsQuery"/>.</summary>
public sealed class SearchAssetsValidator : AbstractValidator<SearchAssetsQuery>
{
    /// <summary>Defines the validation rules.</summary>
    public SearchAssetsValidator()
    {
        RuleFor(query => query.OfficeId)
            .Must(id => id is null || !id.Value.IsEmpty)
            .WithMessage("Office is required when provided.");
        RuleFor(query => query.CategoryId)
            .Must(id => id is null || !id.Value.IsEmpty)
            .WithMessage("Category is required when provided.");
        RuleFor(query => query.Status)
            .Must(status => status is null || Enum.IsDefined(status.Value))
            .WithMessage("Status must be a valid asset status.");
        RuleFor(query => query.SearchText).MaximumLength(100);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

/// <summary>Handles <see cref="SearchAssetsQuery"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="OfficeHierarchy">Office hierarchy domain service.</param>
public sealed class SearchAssetsHandler(
    IAssetRepository AssetRepository,
    IOfficeRepository OfficeRepository,
    ICategoryRepository CategoryRepository,
    IOfficeHierarchy OfficeHierarchy) : IQueryHandler<SearchAssetsQuery, PagedResult<AssetListItemDto>>
{
    /// <inheritdoc />
    public async Task<ErrorOr<PagedResult<AssetListItemDto>>> HandleAsync(SearchAssetsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OfficeId>? officeIdsIncludingDescendants = null;
        if (query.OfficeId is { } officeId && query.IncludeDescendantOffices)
        {
            officeIdsIncludingDescendants = await OfficeHierarchy.CollectOfficeAndDescendantsAsync(officeId, cancellationToken);
        }

        var filter = new AssetSearchFilter(
            query.OfficeId,
            officeIdsIncludingDescendants,
            query.CategoryId,
            query.Status,
            query.SearchText?.Trim(),
            query.Page,
            query.PageSize);

        var (items, total) = await AssetRepository.SearchAsync(filter, cancellationToken);

        var officeNames = (await OfficeRepository.ListAllAsync(cancellationToken))
            .ToDictionary(office => office.Id, office => office.Name);
        var categoryNames = (await CategoryRepository.ListAsync(cancellationToken))
            .ToDictionary(category => category.Id, category => category.Name);

        var itemsDto = items
            .Select(asset => asset.ToListItemDto(
                officeNames.GetValueOrDefault(asset.OfficeId),
                categoryNames.GetValueOrDefault(asset.CategoryId)))
            .ToList();

        return new PagedResult<AssetListItemDto>(itemsDto, total, query.Page, query.PageSize);
    }
}
