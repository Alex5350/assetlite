using AssetLite.Domain.Assets;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Shared EF Core value converters for the Domain's typed ids and value objects.
/// </summary>
/// <remarks>
/// <para>
/// All strongly typed ids (<see cref="AssetId"/>, <see cref="OfficeId"/>, <see cref="CategoryId"/>,
/// <see cref="AssignmentId"/>) are record structs wrapping a <see cref="Guid"/> (sequential Guid v7),
/// persisted as BLOB/TEXT per the provider's default Guid mapping.
/// </para>
/// <para>
/// <see cref="AssetTag"/> is stored as its canonical fixed-width string (<c>AST-000123</c>) rather
/// than the numeric <see cref="AssetTag.Number"/>: because tags are zero-padded to exactly six
/// digits, lexicographic ordering on the stored string equals numeric ordering, so
/// <c>ORDER BY</c>/<c>MAX</c>-style queries (tag allocation, deterministic paging) and
/// case-insensitive <c>LIKE '%...%'</c> tag search both translate directly to SQLite without
/// casting. The unique index on the column is the concurrency backstop for tag allocation.
/// </para>
/// </remarks>
public static class EfConverters
{
    /// <summary>Converter for <see cref="AssetId"/>.</summary>
    public static readonly ValueConverter<AssetId, Guid> AssetIdConverter = new(
        id => id.Value,
        value => new AssetId(value));

    /// <summary>Converter for <see cref="OfficeId"/>.</summary>
    public static readonly ValueConverter<OfficeId, Guid> OfficeIdConverter = new(
        id => id.Value,
        value => new OfficeId(value));

    /// <summary>Converter for a nullable <see cref="OfficeId"/> (self-referencing parent FK).</summary>
    public static readonly ValueConverter<OfficeId?, Guid?> NullableOfficeIdConverter = new(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new OfficeId(value.Value) : null);

    /// <summary>Converter for <see cref="CategoryId"/>.</summary>
    public static readonly ValueConverter<CategoryId, Guid> CategoryIdConverter = new(
        id => id.Value,
        value => new CategoryId(value));

    /// <summary>Converter for <see cref="AssignmentId"/>.</summary>
    public static readonly ValueConverter<AssignmentId, Guid> AssignmentIdConverter = new(
        id => id.Value,
        value => new AssignmentId(value));

    /// <summary>Converter for <see cref="AssetTag"/> (canonical string storage, see remarks above).</summary>
    public static readonly ValueConverter<AssetTag, string> AssetTagConverter = new(
        tag => tag.Value,
        value => FromDb(value));

    private static AssetTag FromDb(string value) =>
        AssetTag.TryParse(value, out var tag)
            ? tag!
            : throw new InvalidOperationException($"Column 'Tag' contains a value that is not a canonical asset tag: '{value}'.");
}
