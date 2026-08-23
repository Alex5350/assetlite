using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Retires an asset (any non-retired, non-disposed status); closes any open assignment.</summary>
/// <param name="AssetId">The asset.</param>
public sealed record RetireAssetCommand(AssetId AssetId) : ICommand;

/// <summary>FluentValidation validator for <see cref="RetireAssetCommand"/>.</summary>
public sealed class RetireAssetValidator : AbstractValidator<RetireAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public RetireAssetValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
    }
}

/// <summary>Handles <see cref="RetireAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
/// <param name="DomainEventDispatcher">Domain event dispatcher.</param>
public sealed class RetireAssetHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider,
    IDomainEventDispatcher DomainEventDispatcher) : ICommandHandler<RetireAssetCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(RetireAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.Retire(DateTimeProvider.UtcNow);
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
