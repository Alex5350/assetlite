using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Disposes a retired asset. Terminal transition (Retired → Disposed only).</summary>
/// <param name="AssetId">The asset.</param>
public sealed record DisposeAssetCommand(AssetId AssetId) : ICommand;

/// <summary>FluentValidation validator for <see cref="DisposeAssetCommand"/>.</summary>
public sealed class DisposeAssetValidator : AbstractValidator<DisposeAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public DisposeAssetValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
    }
}

/// <summary>Handles <see cref="DisposeAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class DisposeAssetHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork) : ICommandHandler<DisposeAssetCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(DisposeAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.Dispose();
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await AssetRepository.UpdateAsync(asset, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return new Success();
    }
}
