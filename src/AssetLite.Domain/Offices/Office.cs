using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;

namespace AssetLite.Domain.Offices;

/// <summary>
/// An office in the organization's hierarchy: a single root/headquarters office governing
/// sub-offices down to individual rooms (HQ → region → site → room).
/// </summary>
/// <remarks>
/// This aggregate validates only its own shape (name, code). Rules that require persisted
/// state — acyclicity, maximum depth, moving under a descendant, code uniqueness, single root —
/// are enforced through the <see cref="IOfficeHierarchy"/> domain service and repository ports
/// in the Application layer.
/// </remarks>
public sealed class Office
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Minimum length of <see cref="Code"/>.</summary>
    public const int CodeMinLength = 3;

    /// <summary>Maximum length of <see cref="Code"/>.</summary>
    public const int CodeMaxLength = 8;

    /// <summary>
    /// Maximum hierarchy depth counting the root: HQ (1) → region (2) → site (3) → room (4).
    /// </summary>
    public const int MaxHierarchyDepth = 4;

    private Office(OfficeId id, string name, string code, OfficeId? parentOfficeId)
    {
        Id = id;
        Name = name;
        Code = code;
        ParentOfficeId = parentOfficeId;
    }

#pragma warning disable CS8618 // EF Core materializes aggregates through the private parameterless constructor.
    private Office()
    {
    }
#pragma warning restore CS8618

    /// <summary>Gets the unique identifier of the office.</summary>
    public OfficeId Id { get; private set; }

    /// <summary>Gets the display name (trimmed, 1 - <see cref="NameMaxLength"/> characters).</summary>
    public string Name { get; private set; }

    /// <summary>Gets the short code (3 - 8 uppercase alphanumeric characters).</summary>
    public string Code { get; private set; }

    /// <summary>Gets the parent office id, or <see langword="null"/> when this is the root (HQ) office.</summary>
    public OfficeId? ParentOfficeId { get; private set; }

    /// <summary>
    /// Creates a new office after validating its shape (name and code). Hierarchy rules are
    /// verified by the caller via <see cref="IOfficeHierarchy"/>.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="code">Short code, 3-8 uppercase alphanumeric characters.</param>
    /// <param name="parentOfficeId">Parent office id, or <see langword="null"/> to create a root office.</param>
    /// <returns>
    /// A successful result with the office, or <see cref="OfficeErrors.InvalidName"/>,
    /// <see cref="OfficeErrors.InvalidCode"/> or <see cref="OfficeErrors.InvalidParent"/>.
    /// </returns>
    public static DomainResult<Office> Create(string name, string code, OfficeId? parentOfficeId)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (normalizedName.Length is < 1 or > NameMaxLength)
        {
            return DomainResult<Office>.Failure(OfficeErrors.InvalidName);
        }

        var normalizedCode = (code ?? string.Empty).Trim();
        if (!IsValidCode(normalizedCode))
        {
            return DomainResult<Office>.Failure(OfficeErrors.InvalidCode);
        }

        if (parentOfficeId is { IsEmpty: true })
        {
            return DomainResult<Office>.Failure(OfficeErrors.InvalidParent);
        }

        return DomainResult<Office>.Success(new Office(OfficeId.New(), normalizedName, normalizedCode, parentOfficeId));
    }

    /// <summary>
    /// Re-parents the office. Callers must first verify hierarchy invariants (no cycles, no move
    /// under own descendants, depth limit) through <see cref="IOfficeHierarchy"/>.
    /// </summary>
    /// <param name="newParentOfficeId">The new parent office id, or <see langword="null"/> to make this office the root.</param>
    /// <returns>
    /// A successful result, or <see cref="OfficeErrors.InvalidParent"/> or
    /// <see cref="OfficeErrors.CannotBeOwnParent"/>.
    /// </returns>
    public DomainResult Reparent(OfficeId? newParentOfficeId)
    {
        if (newParentOfficeId is { IsEmpty: true })
        {
            return DomainResult.Failure(OfficeErrors.InvalidParent);
        }

        if (newParentOfficeId == Id)
        {
            return DomainResult.Failure(OfficeErrors.CannotBeOwnParent);
        }

        ParentOfficeId = newParentOfficeId;
        return DomainResult.Success();
    }

    private static bool IsValidCode(string code)
    {
        if (code.Length is < CodeMinLength or > CodeMaxLength)
        {
            return false;
        }

        foreach (var character in code)
        {
            if (character is not ((>= 'A' and <= 'Z') or (>= '0' and <= '9')))
            {
                return false;
            }
        }

        return true;
    }
}
