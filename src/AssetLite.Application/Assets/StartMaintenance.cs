using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Moves an asset into maintenance (from InStock or Assigned).</summary>
/// <param name="AssetId">The asset.</param>
public sealed record StartMaintenanceCommand(AssetId AssetId) : ICommand;

/// <summary>FluentValidation validator for <see cref="StartMaintenanceCommand"/>.</summary>
public sealed class StartMaintenanceValidator : AbstractValidator<StartMaintenanceCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public StartMaintenanceValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
    }
}

/// <summary>Handles <see cref="StartMaintenanceCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
public sealed class StartMaintenanceHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider) : ICommandHandler<StartMaintenanceCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(StartMaintenanceCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.StartMaintenance(DateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return new Success();
    }
}
