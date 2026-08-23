using AssetLite.Application.Abstractions;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Offices;

/// <summary>Re-parents an existing office, verifying cycle and depth rules first.</summary>
/// <param name="OfficeId">The office to move.</param>
/// <param name="NewParentOfficeId">The new parent office (must exist).</param>
public sealed record MoveOfficeCommand(OfficeId OfficeId, OfficeId NewParentOfficeId) : ICommand;

/// <summary>FluentValidation validator for <see cref="MoveOfficeCommand"/>.</summary>
public sealed class MoveOfficeValidator : AbstractValidator<MoveOfficeCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public MoveOfficeValidator()
    {
        RuleFor(command => command.OfficeId).Must(id => !id.IsEmpty).WithMessage("Office is required.");
        RuleFor(command => command.NewParentOfficeId).Must(id => !id.IsEmpty).WithMessage("Parent office is required.");
    }
}

/// <summary>Handles <see cref="MoveOfficeCommand"/>.</summary>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="OfficeHierarchy">Office hierarchy domain service.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class MoveOfficeHandler(
    IOfficeRepository OfficeRepository,
    IOfficeHierarchy OfficeHierarchy,
    IUnitOfWork UnitOfWork) : ICommandHandler<MoveOfficeCommand>
{
    /// <inheritdoc />
    public async Task<ErrorOr<Success>> HandleAsync(MoveOfficeCommand command, CancellationToken cancellationToken = default)
    {
        var office = await OfficeRepository.GetByIdAsync(command.OfficeId, cancellationToken);
        if (office is null)
        {
            return OfficeErrors.NotFound.ToError();
        }

        if (await OfficeRepository.GetByIdAsync(command.NewParentOfficeId, cancellationToken) is null)
        {
            return OfficeErrors.NotFound.ToError();
        }

        // Depth, self-parenting and descendant (cycle) checks before mutating the aggregate.
        var hierarchy = await OfficeHierarchy.EnsureValidParentAsync(office.Id, command.NewParentOfficeId, cancellationToken);
        if (hierarchy.IsFailure)
        {
            return hierarchy.ToError();
        }

        var result = office.Reparent(command.NewParentOfficeId);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await OfficeRepository.UpdateAsync(office, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return new Success();
    }
}
