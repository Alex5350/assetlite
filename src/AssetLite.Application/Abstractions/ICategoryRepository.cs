using AssetLite.Domain.Categories;
using AssetLite.Domain.Identities;

namespace AssetLite.Application.Abstractions;

/// <summary>Persistence port for the <see cref="AssetCategory"/> configuration entity.</summary>
public interface ICategoryRepository
{
    /// <summary>Loads a single category by id.</summary>
    /// <param name="id">The category id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category, or <see langword="null"/> when not found.</returns>
    Task<AssetCategory?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>Loads all categories.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All categories.</returns>
    Task<IReadOnlyList<AssetCategory>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new category for insertion.</summary>
    /// <param name="category">The category to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAsync(AssetCategory category, CancellationToken cancellationToken = default);

    /// <summary>Stages changes to an existing category.</summary>
    /// <param name="category">The category to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateAsync(AssetCategory category, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a category with the given name already exists (case-insensitive).</summary>
    /// <param name="name">The category name (trimmed).</param>
    /// <param name="excludeCategoryId">Optional category id to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    Task<bool> NameExistsAsync(string name, CategoryId? excludeCategoryId = null, CancellationToken cancellationToken = default);
}
