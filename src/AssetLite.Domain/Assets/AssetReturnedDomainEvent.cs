using AssetLite.Domain.Common;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Domain.Assets;

/// <summary>Raised when an assigned asset is returned to stock.</summary>
/// <param name="AssetId">The returned asset.</param>
/// <param name="Tag">The asset's tag.</param>
/// <param name="ReturnedAtUtc">The UTC moment of return.</param>
public sealed record AssetReturnedDomainEvent(
    AssetId AssetId,
    AssetTag Tag,
    DateTimeOffset ReturnedAtUtc) : IDomainEvent;
