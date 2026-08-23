using AssetLite.Application.Abstractions;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using Microsoft.EntityFrameworkCore;

namespace AssetLite.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IOfficeRepository"/> over the Offices table.</summary>
internal sealed class OfficeRepository(AssetLiteDbContext dbContext) : IOfficeRepository
{
    /// <inheritdoc />
    public Task<Office?> GetByIdAsync(OfficeId id, CancellationToken cancellationToken = default) =>
        dbContext.Offices.FirstOrDefaultAsync(office => office.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Office?> GetRootAsync(CancellationToken cancellationToken = default) =>
        dbContext.Offices.FirstOrDefaultAsync(office => office.ParentOfficeId == null, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Office>> ListChildrenAsync(OfficeId parentId, CancellationToken cancellationToken = default) =>
        await dbContext.Offices
            .Where(office => office.ParentOfficeId != null && office.ParentOfficeId == parentId)
            .OrderBy(office => office.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Office>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Offices
            .OrderBy(office => office.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasChildrenAsync(OfficeId id, CancellationToken cancellationToken = default) =>
        dbContext.Offices.AsNoTracking().AnyAsync(office => office.ParentOfficeId == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasAssetsAsync(OfficeId id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().AnyAsync(asset => asset.OfficeId == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CodeExistsAsync(string code, OfficeId? excludeOfficeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        var query = dbContext.Offices.AsNoTracking().Where(office => office.Code == normalized);
        if (excludeOfficeId is { } excluded)
        {
            query = query.Where(office => office.Id != excluded);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Office office, CancellationToken cancellationToken = default) =>
        await dbContext.Offices.AddAsync(office, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(Office office, CancellationToken cancellationToken = default)
    {
        dbContext.Offices.Update(office);
        return Task.CompletedTask;
    }
}
