using AssetLite.Application.Abstractions;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Errors;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Categories;

/// <summary>Creates a new asset category.</summary>
/// <param name="Name">Display name (unique).</param>
/// <param name="Description">Optional description.</param>
/// <param name="ExpectedLifespanMonths">Expected lifespan in months (positive).</param>
public sealed record CreateCategoryCommand(string Name, string? Description, int ExpectedLifespanMonths) : ICommand;

/// <summary>FluentValidation validator for <see cref="CreateCategoryCommand"/>.</summary>
public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AssetCategory.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(AssetCategory.DescriptionMaxLength);
        RuleFor(command => command.ExpectedLifespanMonths).GreaterThan(0);
    }
}

/// <summary>Handles <see cref="CreateCategoryCommand"/>.</summary>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class CreateCategoryHandler(
    ICategoryRepository CategoryRepository,
    IUnitOfWork UnitOfWork) : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<CategoryDto>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (await CategoryRepository.NameExistsAsync(command.Name, cancellationToken: cancellationToken))
        {
            return CategoryErrors.DuplicateName.ToError();
        }

        var result = AssetCategory.Create(command.Name, command.Description, command.ExpectedLifespanMonths);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        var category = result.GetValueOrThrow();
        await CategoryRepository.AddAsync(category, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }
}
