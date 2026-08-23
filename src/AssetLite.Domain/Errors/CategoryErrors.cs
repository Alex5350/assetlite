using AssetLite.Domain.Common;

namespace AssetLite.Domain.Errors;

/// <summary>
/// Stable error catalog for the <see cref="AssetLite.Domain.Categories.AssetCategory"/>
/// configuration entity. All codes are prefixed <c>"Category."</c>.
/// </summary>
public static class CategoryErrors
{
    /// <summary>No category with the given id exists ("Category.NotFound").</summary>
    public static readonly DomainError NotFound = new("Category.NotFound", "Category was not found.");

    /// <summary>Category name empty or too long ("Category.InvalidName").</summary>
    public static readonly DomainError InvalidName = new(
        "Category.InvalidName",
        $"Category name is required and must be at most {AssetLite.Domain.Categories.AssetCategory.NameMaxLength} characters.");

    /// <summary>Category description too long ("Category.InvalidDescription").</summary>
    public static readonly DomainError InvalidDescription = new(
        "Category.InvalidDescription",
        $"Category description must be at most {AssetLite.Domain.Categories.AssetCategory.DescriptionMaxLength} characters.");

    /// <summary>Expected lifespan not a positive number of months ("Category.InvalidLifespan").</summary>
    public static readonly DomainError InvalidLifespan = new(
        "Category.InvalidLifespan",
        "Expected lifespan must be a positive number of months.");

    /// <summary>Another category already uses the name ("Category.DuplicateName").</summary>
    public static readonly DomainError DuplicateName = new(
        "Category.DuplicateName",
        "A category with this name already exists.");
}
