using AssetLite.Domain.Common;

namespace AssetLite.Application.Abstractions;

/// <summary>
/// Dispatches domain events pulled from aggregates after changes were persisted. The
/// infrastructure layer decides the mechanism (in-process handlers, outbox, ...).
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches the given events.</summary>
    /// <param name="events">Events previously pulled from aggregates via <c>PullEvents()</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the dispatch operation.</returns>
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);
}
