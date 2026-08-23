namespace AssetLite.Domain.Enums;

/// <summary>The lifecycle status of a physical asset.</summary>
/// <remarks>
/// Allowed transitions are enforced by the
/// <see cref="AssetLite.Domain.Assets.Asset"/> aggregate: InStock ⇄ Assigned,
/// InStock/Assigned → Maintenance → InStock, any active status → Retired → Disposed.
/// </remarks>
public enum AssetStatus
{
    /// <summary>Stored in stock; available for assignment.</summary>
    InStock = 1,

    /// <summary>Currently assigned to a person (has an open assignment record).</summary>
    Assigned = 2,

    /// <summary>Under maintenance or repair.</summary>
    Maintenance = 3,

    /// <summary>Withdrawn from active use; can only be disposed.</summary>
    Retired = 4,

    /// <summary>Permanently removed from the inventory lifecycle.</summary>
    Disposed = 5,
}
