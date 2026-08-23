using AssetLite.Application.Abstractions;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Application.Categories;

/// <summary>Updates an existing asset category.</summary>
/// <param name="CategoryId">The category to update.</param>
/// <param name="Name">New display name (unique).</param>
/// <param name="Description">New optional description.</param>
/// <param name="ExpectedLifespanMonths">New expected lifespan in months (positive).</param>
public sealed record UpdateCategoryCommand(
    CategoryId CategoryId,
    string Name,
    string? Description,
    int ExpectedLifespanMonths) : ICommand;

/// <summary>FluentValidation validator for <see cref="UpdateCategoryCommand"/>.</summary>
public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public UpdateCategoryValidator()
    {
        RuleFor(command => command.CategoryId).Must(id => !id.IsEmpty).WithMessage("Category is required.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AssetCategory.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(AssetCategory.DescriptionMaxLength);
        RuleFor(command => command.ExpectedLifespanMonths).GreaterThan(0);
    }
}

/// <summary>Handles <see cref="UpdateCategoryCommand"/>.</summary>
/// <param name="CategoryRepository">Category repository port.</param>
/// <param name="UnitOfWork">Unit of work.</param>
public sealed class UpdateCategoryHandler(
    ICategoryRepository CategoryRepository,
    IUnitOfWork UnitOfWork) : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<CategoryDto>> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await CategoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound.ToError();
        }

        if (await CategoryRepository.NameExistsAsync(command.Name, command.CategoryId, cancellationToken))
        {
            return CategoryErrors.DuplicateName.ToError();
        }

        var result = category.Update(command.Name, command.Description, command.ExpectedLifespanMonths);
        if (result.IsFailure)
        {
            return result.ToError();
        }

        await CategoryRepository.UpdateAsync(category, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }
}
