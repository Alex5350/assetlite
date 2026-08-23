using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Offices;
using Microsoft.EntityFrameworkCore;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// EF Core / SQLite mapping for the whole domain model. Configuration lives in
/// <see cref="Persistence.Configurations"/>; this type only discovers it and exposes the sets used
/// by the repository implementations.
/// </summary>
/// <remarks>
/// Domain events (<c>Asset.Events</c>) are never mapped: they are transient by design and are
/// pulled with <c>PullEvents()</c> after a successful commit, then dispatched by
/// <c>IDomainEventDispatcher</c>.
/// </remarks>
public sealed class AssetLiteDbContext(DbContextOptions<AssetLiteDbContext> options) : DbContext(options)
{
    /// <summary>Gets the asset aggregate set (includes the assignment history via auto-include).</summary>
    public DbSet<Asset> Assets => Set<Asset>();

    /// <summary>Gets the office set.</summary>
    public DbSet<Office> Offices => Set<Office>();

    /// <summary>Gets the asset category set.</summary>
    public DbSet<AssetCategory> Categories => Set<AssetCategory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetLiteDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
