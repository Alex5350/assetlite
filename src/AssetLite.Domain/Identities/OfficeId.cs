namespace AssetLite.Domain.Identities;

/// <summary>
/// Strongly typed identifier for the <see cref="AssetLite.Domain.Offices.Office"/> aggregate.
/// </summary>
/// <remarks>
/// Persistence layers should map this with a Guid value converter. Identifiers are created as
/// sequential Guid v7 values (see <see cref="New"/>) for index-friendly generation.
/// </remarks>
public readonly record struct OfficeId(Guid Value)
{
    /// <summary>Creates a new identifier backed by a version 7 (sequential) GUID.</summary>
    /// <returns>A new, unique <see cref="OfficeId"/>.</returns>
    public static OfficeId New() => new(Guid.CreateVersion7());

    /// <summary>Gets a value indicating whether the identifier is unset (<see cref="Guid.Empty"/>).</summary>
    public bool IsEmpty => Value == Guid.Empty;
}
