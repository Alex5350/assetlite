namespace AssetLite.Application.Abstractions;

/// <summary>
/// Unit of work port. Handlers stage changes through repositories and commit once via
/// <see cref="SaveChangesAsync"/>; domain events are dispatched only after a successful commit.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits all staged changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
