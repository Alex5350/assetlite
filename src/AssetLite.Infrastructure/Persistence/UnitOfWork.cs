using AssetLite.Application.Abstractions;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Unit of work over the scoped <see cref="AssetLiteDbContext"/>: repositories stage changes on
/// the tracked graph, handlers commit once, then dispatch pulled domain events.
/// </summary>
internal sealed class UnitOfWork(AssetLiteDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
