using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;

namespace AssetLite.Application.Offices;

/// <summary>Flat office representation.</summary>
/// <param name="Id">Office id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Short code (3-8 uppercase alphanumeric).</param>
/// <param name="ParentOfficeId">Parent office id, or <see langword="null"/> for the root (HQ).</param>
public sealed record OfficeDto(OfficeId Id, string Name, string Code, OfficeId? ParentOfficeId);

/// <summary>Node in the office hierarchy tree; children are ordered by name.</summary>
/// <param name="Id">Office id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Short code.</param>
/// <param name="ParentOfficeId">Parent office id, or <see langword="null"/> for the root (HQ).</param>
/// <param name="Children">Direct child nodes.</param>
public sealed record OfficeTreeNodeDto(
    OfficeId Id,
    string Name,
    string Code,
    OfficeId? ParentOfficeId,
    IReadOnlyList<OfficeTreeNodeDto> Children);

/// <summary>Mapping helpers for office DTOs.</summary>
internal static class OfficeMappings
{
    /// <summary>Maps an office aggregate to its flat DTO.</summary>
    /// <param name="office">The office.</param>
    /// <returns>The DTO.</returns>
    public static OfficeDto ToDto(this Office office) => new(office.Id, office.Name, office.Code, office.ParentOfficeId);
}
