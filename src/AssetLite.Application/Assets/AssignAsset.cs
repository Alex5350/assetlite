using AssetLite.Application.Abstractions;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Assets;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Assets;

/// <summary>Assigns an asset to a person (or reassigns when it is already assigned).</summary>
/// <param name="AssetId">The asset.</param>
/// <param name="AssigneeName">Assignee display name.</param>
/// <param name="AssigneeEmail">Assignee email address.</param>
public sealed record AssignAssetCommand(AssetId AssetId, string AssigneeName, string AssigneeEmail) : ICommand;

/// <summary>FluentValidation validator for <see cref="AssignAssetCommand"/>.</summary>
public sealed class AssignAssetValidator : AbstractValidator<AssignAssetCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public AssignAssetValidator()
    {
        RuleFor(command => command.AssetId).Must(id => !id.IsEmpty).WithMessage("Asset is required.");
        RuleFor(command => command.AssigneeName)
            .NotEmpty()
            .MaximumLength(Asset.AssigneeNameMaxLength);
        RuleFor(command => command.AssigneeEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(Asset.AssigneeEmailMaxLength);
    }
}

/// <summary>Handles <see cref="AssignAssetCommand"/>.</summary>
/// <param name="AssetRepository">Asset repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
/// <param name="DateTimeProvider">Time provider.</param>
/// <param name="DomainEventDispatcher">Domain event dispatcher.</param>
public sealed class AssignAssetHandler(
    IAssetRepository AssetRepository,
    IUnitOfWork UnitOfWork,
    IDateTimeProvider DateTimeProvider,
    IDomainEventDispatcher DomainEventDispatcher) : ICommandHandler<AssignAssetCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(AssignAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await AssetRepository.GetByIdAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return AssetErrors.NotFound.ToError();
        }

        var result = asset.AssignTo(command.AssigneeName, command.AssigneeEmail, DateTimeProvider.UtcNow);
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
