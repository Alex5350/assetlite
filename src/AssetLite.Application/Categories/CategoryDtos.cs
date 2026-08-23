using AssetLite.Domain.Categories;
using AssetLite.Domain.Identities;

namespace AssetLite.Application.Categories;

/// <summary>Asset category representation.</summary>
/// <param name="Id">Category id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="ExpectedLifespanMonths">Expected lifespan in months (positive).</param>
public sealed record CategoryDto(CategoryId Id, string Name, string? Description, int ExpectedLifespanMonths);

/// <summary>Mapping helpers for category DTOs.</summary>
internal static class CategoryMappings
{
    /// <summary>Maps a category entity to its DTO.</summary>
    /// <param name="category">The category.</param>
    /// <returns>The DTO.</returns>
    public static CategoryDto ToDto(this AssetCategory category) =>
        new(category.Id, category.Name, category.Description, category.ExpectedLifespanMonths);
}
