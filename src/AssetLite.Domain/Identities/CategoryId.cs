namespace AssetLite.Domain.Identities;

/// <summary>
/// Strongly typed identifier for the <see cref="AssetLite.Domain.Categories.AssetCategory"/>
/// configuration entity.
/// </summary>
/// <remarks>
/// Persistence layers should map this with a Guid value converter. Identifiers are created as
/// sequential Guid v7 values (see <see cref="New"/>) for index-friendly generation.
/// </remarks>
public readonly record struct CategoryId(Guid Value)
{
    /// <summary>Creates a new identifier backed by a version 7 (sequential) GUID.</summary>
    /// <returns>A new, unique <see cref="CategoryId"/>.</returns>
    public static CategoryId New() => new(Guid.CreateVersion7());

    /// <summary>Gets a value indicating whether the identifier is unset (<see cref="Guid.Empty"/>).</summary>
    public bool IsEmpty => Value == Guid.Empty;
}
