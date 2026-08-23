using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AssetLite.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAssetRepository"/>. The assignment history is always
/// loaded with the aggregate (auto-include on the backing-field navigation).
/// </summary>
internal sealed class AssetRepository(AssetLiteDbContext dbContext) : IAssetRepository
{
    /// <inheritdoc />
    public Task<Asset?> GetByIdAsync(AssetId id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Asset?> GetByTagAsync(AssetTag tag, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(asset => asset.Tag == tag, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TagExistsAsync(AssetTag tag, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().AnyAsync(asset => asset.Tag == tag, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default) =>
        await dbContext.Assets.AddAsync(asset, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        // Tracked aggregates are already staged; this re-attaches detached graphs (e.g. loaded in
        // a different scope) and marks the whole aggregate modified.
        dbContext.Assets.Update(asset);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Asset> Items, int Total)> SearchAsync(
        AssetSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);

        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, int.MaxValue);
        var items = await query
            .OrderBy(asset => asset.Tag) // canonical zero-padded string: lexicographic == numeric order
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private IQueryable<Asset> BuildQuery(AssetSearchFilter filter)
    {
        var query = string.IsNullOrWhiteSpace(filter.SearchText)
            ? dbContext.Assets.AsQueryable()
            : dbContext.Assets.FromSqlInterpolated(TextSearchSql(filter.SearchText.Trim()));

        // OfficeIdsIncludingDescendants takes precedence over OfficeId per the filter contract.
        if (filter.OfficeIdsIncludingDescendants is { Count: > 0 } officeIds)
        {
            var ids = officeIds;
            query = query.Where(asset => ids.Contains(asset.OfficeId));
        }
        else if (filter.OfficeId is { } officeId)
        {
            query = query.Where(asset => asset.OfficeId == officeId);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(asset => asset.CategoryId == categoryId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(asset => asset.Status == status);
        }

        return query;
    }

    /// <summary>
    /// Builds the case-insensitive "contains" predicate over name, serial number, tag and model as
    /// raw composable SQL. The tag column stores the canonical string via a value converter, which
    /// LINQ cannot reach with a string method call, so all four fields are matched uniformly with
    /// <c>instr(lower(col), lower(@p)) &gt; 0</c> (SQLite's lower/instr are ASCII, matching the
    /// case-insensitive contract for canonical tags and names).
    /// </summary>
    private static FormattableString TextSearchSql(string searchText) =>
        $"""
        SELECT *
        FROM Assets
        WHERE instr(lower(Name), lower({searchText})) > 0
           OR instr(lower(Tag), lower({searchText})) > 0
           OR (SerialNumber IS NOT NULL AND instr(lower(SerialNumber), lower({searchText})) > 0)
           OR (Model IS NOT NULL AND instr(lower(Model), lower({searchText})) > 0)
        """;
}
