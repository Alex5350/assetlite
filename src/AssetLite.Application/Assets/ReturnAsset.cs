using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Returns an assigned asset to stock (closes the open assignment).</summary>
/// <param name="AssetId">The asset.</param>
public sealed record ReturnAssetCommand(AssetId AssetId) : ICommand;

/// <summary>FluentValidation validator for <see cref="ReturnAssetCommand"/>.</summary>
public sealed class ReturnAssetValidator : AbstractValidator<ReturnAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public ReturnAssetValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
    }
}

/// <summary>Handles <see cref="ReturnAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
/// <param name="DomainEventDispatcher">Domain event dispatcher.</param>
public sealed class ReturnAssetHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider,
    IDomainEventDispatcher DomainEventDispatcher) : ICommandHandler<ReturnAssetCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(ReturnAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.ReturnToStock(DateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        var events = asset.PullEvents();
        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.DispatchAsync(events, cancellationToken);

        return new Success();
    }
}
