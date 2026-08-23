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
/// Registers a new asset. The asset tag is allocated sequentially by
/// <see cref="IAssetTagAllocator"/> (AST-000001, AST-000002, ...); the asset starts
/// <see cref="AssetStatus.InStock"/>.
/// </summary>
/// <param name="CategoryId">Existing category.</param>
/// <param name="OfficeId">Existing office holding the asset.</param>
/// <param name="Name">Display name.</param>
/// <param name="Condition">Physical condition.</param>
/// <param name="Manufacturer">Optional manufacturer.</param>
/// <param name="Model">Optional model.</param>
/// <param name="SerialNumber">Optional serial number.</param>
/// <param name="PurchaseDate">Optional purchase date (not in the future).</param>
/// <param name="PurchaseCost">Optional purchase cost (non-negative).</param>
/// <param name="Currency">Optional 3-letter currency; defaults to USD.</param>
/// <param name="Notes">Optional free-form notes.</param>
public sealed record RegisterAssetCommand(
    CategoryId CategoryId,
    OfficeId OfficeId,
    string Name,
    AssetCondition Condition,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    DateOnly? PurchaseDate = null,
    decimal? PurchaseCost = null,
    string? Currency = null,
    string? Notes = null) : ICommand;

/// <summary>FluentValidation validator for <see cref="RegisterAssetCommand"/>.</summary>
public sealed class RegisterAssetValidator : AbstractValidator<RegisterAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    /// <param name="dateTimeProvider">Time provider used for the future-date check.</param>
    public RegisterAssetValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.CategoryId).Must(id => !id.IsEmpty).WithMessage("Category is required.");
        RuleFor(command => command.OfficeId).Must(id => !id.IsEmpty).WithMessage("Office is required.");
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

/// <summary>Handles <see cref="RegisterAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="TagAllocator">Sequential tag allocator port.</param>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
public sealed class RegisterAssetHandler(
    IAssetRepository AssetRepository,
    IAssetTagAllocator TagAllocator,
    ICategoryRepository CategoryRepository,
    IOfficeRepository OfficeRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider) : ICommandHandler<RegisterAssetCommand, AssetDetailDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<AssetDetailDto>> HandleAsync(RegisterAssetCommand command, CancellationToken cancellationToken = default)
    {
        var category = await CategoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound.ToError();
        }

        var office = await OfficeRepository.GetByIdAsync(command.OfficeId, cancellationToken);
        if (office is null)
        {
            return OfficeErrors.NotFound.ToError();
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

        var tag = await TagAllocator.AllocateAsync(cancellationToken);
        var result = Asset.Create(
            tag,
            command.CategoryId,
            command.OfficeId,
            command.Name,
            command.Condition,
            DateTimeProvider.UtcNow,
            command.Manufacturer,
            command.Model,
            command.SerialNumber,
            command.PurchaseDate,
            purchaseCost,
            command.Notes);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        var asset = result.GetValueOrThrow();
        await AssetRepository.AddAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return asset.ToDetailDto(office.Name, category.Name);
    }
}
