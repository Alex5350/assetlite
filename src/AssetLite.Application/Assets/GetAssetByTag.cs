using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Fetches a single asset by its tag, including the full assignment history.</summary>
/// <param name="Tag">Canonical tag, e.g. AST-000123.</param>
public sealed record GetAssetByTagQuery(string Tag) : IQuery<AssetDetailDto>;

/// <summary>FluentValidation validator for <see cref="GetAssetByTagQuery"/>.</summary>
public sealed class GetAssetByTagValidator : AbstractValidator<GetAssetByTagQuery>
{
    /// <summary>Defines the validation rules.</summary>
    public GetAssetByTagValidator()
    {
        RuleFor(query => query.Tag)
            .NotEmpty()
            .Matches("^AST-[0-9]{6}$")
            .WithMessage("Asset tag must be in the format AST-000123.");
    }
}

/// <summary>Handles <see cref="GetAssetByTagQuery"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
public sealed class GetAssetByTagHandler(
    IAssetRepository AssetRepository,
    IOfficeRepository OfficeRepository,
    ICategoryRepository CategoryRepository) : IQueryHandler<GetAssetByTagQuery, AssetDetailDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<AssetDetailDto>> HandleAsync(GetAssetByTagQuery query, CancellationToken cancellationToken = default)
    {
        if (!AssetTag.TryParse(query.Tag, out var tag) || tag is null)
        {
            return ValueObjectErrors.InvalidAssetTag.ToError();
        }

        var asset = await AssetRepository.GetByTagAsync(tag, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var officeName = (await OfficeRepository.GetByIdAsync(asset.OfficeId, cancellationToken))?.Name;
        var categoryName = (await CategoryRepository.GetByIdAsync(asset.CategoryId, cancellationToken))?.Name;

        return asset.ToDetailDto(officeName, categoryName);
    }
}
