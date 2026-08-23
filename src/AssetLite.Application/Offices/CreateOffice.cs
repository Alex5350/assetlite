using AssetLite.Application.Abstractions;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Offices;

/// <summary>Creates a new office. With no parent it becomes the root (HQ); only one root is allowed.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Code">Short code, 3-8 uppercase alphanumeric characters.</param>
/// <param name="ParentOfficeId">Parent office, or <see langword="null"/> to create the root HQ.</param>
public sealed record CreateOfficeCommand(string Name, string Code, OfficeId? ParentOfficeId) : ICommand;

/// <summary>FluentValidation validator for <see cref="CreateOfficeCommand"/>.</summary>
public sealed class CreateOfficeValidator : AbstractValidator<CreateOfficeCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public CreateOfficeValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Office.NameMaxLength);
        RuleFor(command => command.Code)
            .NotEmpty()
            .Matches("^[A-Z0-9]{3,8}$")
            .WithMessage("Office code must be 3-8 uppercase alphanumeric characters.");
    }
}

/// <summary>Handles <see cref="CreateOfficeCommand"/>.</summary>
/// <param name="OfficeRepository">Office repository port.</param>
/// <param name="OfficeHierarchy">Office hierarchy domain service.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class CreateOfficeHandler(
    IOfficeRepository OfficeRepository,
    IOfficeHierarchy OfficeHierarchy,
    IUnitOfWork UnitOfWork) : ICommandHandler<CreateOfficeCommand, OfficeDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<OfficeDto>> HandleAsync(CreateOfficeCommand command, CancellationToken cancellationToken = default)
    {
        var result = Office.Create(command.Name, command.Code, command.ParentOfficeId);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        var office = result.GetValueOrThrow();

        if (office.ParentOfficeId is null)
        {
            // Single-root invariant: at most one HQ.
            if (await OfficeRepository.GetRootAsync(cancellationToken) is not null)
            {
                return OfficeErrors.RootAlreadyExists.ToError();
            }
        }
        else
        {
            if (await OfficeRepository.GetByIdAsync(office.ParentOfficeId.Value, cancellationToken) is null)
            {
                return OfficeErrors.NotFound.ToError();
            }

            var hierarchy = await OfficeHierarchy.EnsureValidParentAsync(office.Id, office.ParentOfficeId, cancellationToken);
            if (hierarchy.IsFailure)
            {
                return hierarchy.ToError();
            }
        }

        if (await OfficeRepository.CodeExistsAsync(office.Code, cancellationToken: cancellationToken))
        {
            return OfficeErrors.DuplicateCode.ToError();
        }

        await OfficeRepository.AddAsync(office, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return office.ToDto();
    }
}
