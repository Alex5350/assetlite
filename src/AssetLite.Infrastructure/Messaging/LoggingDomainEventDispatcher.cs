using AssetLite.Application.Abstractions;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AssetLite.Infrastructure.Messaging;

/// <summary>
/// Simple dispatching pipeline for domain events: every event is logged with structured fields
/// at Information level. Asset lifecycle events are audit-flavored side effects, so a log-only
/// dispatcher covers the current needs; swapping in an outbox later only requires replacing this
/// registration.
/// </summary>
internal sealed class LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger) : IDomainEventDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case AssetAssignedDomainEvent assigned:
                    logger.LogInformation(
                        "Domain event {EventType}: asset {AssetId} ({Tag}) assigned to {AssigneeName} <{AssigneeEmail}> at {AssignedAtUtc:O}",
                        nameof(AssetAssignedDomainEvent),
                        assigned.AssetId.Value,
                        assigned.Tag.Value,
                        assigned.AssigneeName,
                        assigned.AssigneeEmail,
                        assigned.AssignedAtUtc);
                    break;

                case AssetReturnedDomainEvent returned:
                    logger.LogInformation(
                        "Domain event {EventType}: asset {AssetId} ({Tag}) returned to stock at {ReturnedAtUtc:O}",
                        nameof(AssetReturnedDomainEvent),
                        returned.AssetId.Value,
                        returned.Tag.Value,
                        returned.ReturnedAtUtc);
                    break;

                case AssetRetiredDomainEvent retired:
                    logger.LogInformation(
                        "Domain event {EventType}: asset {AssetId} ({Tag}) retired at {RetiredAtUtc:O}",
                        nameof(AssetRetiredDomainEvent),
                        retired.AssetId.Value,
                        retired.Tag.Value,
                        retired.RetiredAtUtc);
                    break;

                default:
                    logger.LogInformation(
                        "Domain event {EventType} raised: {@DomainEvent}",
                        domainEvent.GetType().Name,
                        domainEvent);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
