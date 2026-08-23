using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Transfers an asset to another office. The target office must exist.</summary>
/// <param name="AssetId">The asset.</param>
/// <param name="TargetOfficeId">The destination office.</param>
public sealed record TransferAssetCommand(AssetId AssetId, OfficeId TargetOfficeId) : ICommand;

/// <summary>FluentValidation validator for <see cref="TransferAssetCommand"/>.</summary>
public sealed class TransferAssetValidator : AbstractValidator<TransferAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public TransferAssetValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
        RuleFor(command => command.TargetOfficeId).Must(id => !id.IsEmpty).WithMessage("Target office is required.");
    }
}

/// <summary>Handles <see cref="TransferAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class TransferAssetHandler(
    IAssetRepository AssetRepository,
    IOfficeRepository OfficeRepository,
    IUnitOfWork UnitOfWork) : ICommandHandler<TransferAssetCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(TransferAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        // Application verifies the target; the aggregate trusts the passed flag.
        if (await OfficeRepository.GetByIdAsync(command.TargetOfficeId, cancellationToken) is null)
        {
            return OfficeErrors.NotFound.ToError();
        }

        var result = asset.TransferTo(command.TargetOfficeId, targetIsValid: true);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return new Success();
    }
}
