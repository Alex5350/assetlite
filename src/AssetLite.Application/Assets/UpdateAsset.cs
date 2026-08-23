using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>
/// Replaces an existing asset's descriptive details (name, specs, notes, category, condition and
/// purchase data). The tag, office, status and assignment history are managed by their own
/// commands; disposed assets are immutable.
/// </summary>
/// <param name="AssetId">The asset to update.</param>
/// <param name="CategoryId">New category (must exist).</param>
/// <param name="Name">New display name.</param>
/// <param name="Condition">New physical condition.</param>
/// <param name="Manufacturer">New optional manufacturer.</param>
/// <param name="Model">New optional model.</param>
/// <param name="SerialNumber">New optional serial number.</param>
/// <param name="PurchaseDate">New optional purchase date (not in the future).</param>
/// <param name="PurchaseCost">New optional purchase cost (non-negative).</param>
/// <param name="Currency">Optional 3-letter currency; defaults to USD.</param>
/// <param name="Notes">New optional free-form notes.</param>
public sealed record UpdateAssetCommand(
    AssetId AssetId,
    CategoryId CategoryId,
    string Name,
    AssetCondition Condition,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    DateOnly? PurchaseDate = null,
    decimal? PurchaseCost = null,
    string? Currency = null,
    string? Notes = null) : ICommand;

/// <summary>FluentValidation validator for <see cref="UpdateAssetCommand"/>.</summary>
public sealed class UpdateAssetValidator : AbstractValidator<UpdateAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    /// <param name="dateTimeProvider">Time provider used for the future-date check.</param>
    public UpdateAssetValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
        RuleFor(command => command.CategoryId).Must(id => !id.IsEmpty).WithMessage("Category is required.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Asset.NameMaxLength);
        RuleFor(command => command.Manufacturer).MaximumLength(Asset.MetadataMaxLength);
        RuleFor(command => command.Model).MaximumLength(Asset.MetadataMaxLength);
        RuleFor(command => command.SerialNumber).MaximumLength(Asset.MetadataMaxLength);
        RuleFor(command => command.Notes).MaximumLength(Asset.NotesMaxLength);
        RuleFor(command => command.Condition).IsInEnum();
        RuleFor(command => command.PurchaseDate)
            .Must(date => date is null || date <= dateTimeProvider.Today)
            .WithMessage("Purchase date cannot be in the future.");
        RuleFor(command => command.PurchaseCost)
            .Must(cost => cost is null || cost >= 0m)
            .WithMessage("Purchase cost cannot be negative.");
        RuleFor(command => command.Currency)
            .Matches("^[A-Z]{3}$")
            .When(command => !string.IsNullOrWhiteSpace(command.Currency))
            .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD).");
    }
}

/// <summary>Handles <see cref="UpdateAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="OfficeRepository">Office repository port (office name for the response DTO).</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
public sealed class UpdateAssetHandler(
    IAssetRepository AssetRepository,
    ICategoryRepository CategoryRepository,
    IOfficeRepository OfficeRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider) : ICommandHandler<UpdateAssetCommand, AssetDetailDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<AssetDetailDto>> HandleAsync(UpdateAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var category = await CategoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound.ToError();
        }

        Money? purchaseCost = null;
        if (command.PurchaseCost is { } amount)
        {
            var money = Money.Create(amount, command.Currency);
            if (money.IsFailure)
            {
                return money.ToError();
            }

            purchaseCost = money.GetValueOrThrow();
        }

        var result = asset.UpdateDetails(
            command.Name,
            command.CategoryId,
            command.Condition,
            command.PurchaseDate,
            purchaseCost,
            DateTimeProvider.UtcNow,
            command.Manufacturer,
            command.Model,
            command.SerialNumber,
            command.Notes);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        var officeName = (await OfficeRepository.GetByIdAsync(asset.OfficeId, cancellationToken))?.Name;
        return asset.ToDetailDto(officeName, category.Name);
    }
}
