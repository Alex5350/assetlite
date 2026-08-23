using AssetLite.Domain.Common;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Domain.Assets;

/// <summary>Raised when an asset is retired (withdrawn from active use).</summary>
/// <param name="AssetId">The retired asset.</param>
/// <param name="Tag">The asset's tag.</param>
/// <param name="RetiredAtUtc">The UTC moment of retirement.</param>
public sealed record AssetRetiredDomainEvent(
    AssetId AssetId,
    AssetTag Tag,
    DateTimeOffset RetiredAtUtc) : IDomainEvent;
