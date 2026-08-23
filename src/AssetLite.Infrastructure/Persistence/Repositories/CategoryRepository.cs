using AssetLite.Application.Abstractions;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Identities;
using Microsoft.EntityFrameworkCore;

namespace AssetLite.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICategoryRepository"/> over the Categories table.</summary>
internal sealed class CategoryRepository(AssetLiteDbContext dbContext) : ICategoryRepository
{
    /// <inheritdoc />
    public Task<AssetCategory?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AssetCategory>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(AssetCategory category, CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AddAsync(category, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(AssetCategory category, CancellationToken cancellationToken = default)
    {
        dbContext.Categories.Update(category);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(string name, CategoryId? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var normalized = (name ?? string.Empty).Trim().ToLowerInvariant();
        var query = dbContext.Categories.AsNoTracking()
            .Where(category => category.Name.ToLower() == normalized);
        if (excludeCategoryId is { } excluded)
        {
            query = query.Where(category => category.Id != excluded);
        }

        return query.AnyAsync(cancellationToken);
    }
}
