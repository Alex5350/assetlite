using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Returns an asset from maintenance back to stock.</summary>
/// <param name="AssetId">The asset.</param>
public sealed record ResumeFromMaintenanceCommand(AssetId AssetId) : ICommand;

/// <summary>FluentValidation validator for <see cref="ResumeFromMaintenanceCommand"/>.</summary>
public sealed class ResumeFromMaintenanceValidator : AbstractValidator<ResumeFromMaintenanceCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public ResumeFromMaintenanceValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
    }
}

/// <summary>Handles <see cref="ResumeFromMaintenanceCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class ResumeFromMaintenanceHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork) : ICommandHandler<ResumeFromMaintenanceCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(ResumeFromMaintenanceCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.ResumeFromMaintenance();
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return new Success();
    }
}
