using AssetLite.Domain.Common;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Domain.Assets;

/// <summary>
/// Raised when an asset is assigned (or reassigned) to a person.
/// Reassignments first close the previous open assignment, then raise this event for the new one.
/// </summary>
/// <param name="AssetId">The assigned asset.</param>
/// <param name="Tag">The asset's tag.</param>
/// <param name="AssigneeName">The assignee's display name.</param>
/// <param name="AssigneeEmail">The assignee's email address.</param>
/// <param name="AssignedAtUtc">The UTC moment of assignment.</param>
public sealed record AssetAssignedDomainEvent(
    AssetId AssetId,
    AssetTag Tag,
    string AssigneeName,
    string AssigneeEmail,
    DateTimeOffset AssignedAtUtc) : IDomainEvent;
