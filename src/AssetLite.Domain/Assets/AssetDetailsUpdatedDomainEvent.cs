using AssetLite.Domain.Common;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Domain.Assets;

/// <summary>Raised when an asset's descriptive details (name, specs, category, purchase data) are edited.</summary>
/// <param name="AssetId">The updated asset.</param>
/// <param name="Tag">The asset's tag.</param>
/// <param name="UpdatedAtUtc">The UTC moment of the edit.</param>
public sealed record AssetDetailsUpdatedDomainEvent(
    AssetId AssetId,
    AssetTag Tag,
    DateTimeOffset UpdatedAtUtc) : IDomainEvent;
