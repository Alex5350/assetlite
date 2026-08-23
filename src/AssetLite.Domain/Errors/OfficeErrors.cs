using AssetLite.Domain.Common;

namespace AssetLite.Domain.Errors;

/// <summary>
/// Stable error catalog for the <see cref="AssetLite.Domain.Offices.Office"/> aggregate and the
/// office hierarchy domain service. All codes are prefixed <c>"Office."</c>.
/// </summary>
public static class OfficeErrors
{
    /// <summary>No office with the given id exists ("Office.NotFound").</summary>
    public static readonly DomainError NotFound = new("Office.NotFound", "Office was not found.");

    /// <summary>The office tree has no root office ("Office.RootNotFound").</summary>
    public static readonly DomainError RootNotFound = new(
        "Office.RootNotFound",
        "No root office (HQ) exists yet.");

    /// <summary>A second root office was requested ("Office.RootAlreadyExists").</summary>
    public static readonly DomainError RootAlreadyExists = new(
        "Office.RootAlreadyExists",
        "A root office (HQ) already exists; a new office requires a parent.");

    /// <summary>Office name empty or too long ("Office.InvalidName").</summary>
    public static readonly DomainError InvalidName = new(
        "Office.InvalidName",
        $"Office name is required and must be at most {AssetLite.Domain.Offices.Office.NameMaxLength} characters.");

    /// <summary>Office code not 3-8 uppercase alphanumeric characters ("Office.InvalidCode").</summary>
    public static readonly DomainError InvalidCode = new(
        "Office.InvalidCode",
        "Office code must be 3-8 uppercase alphanumeric characters (A-Z, 0-9).");

    /// <summary>The parent office id is unset ("Office.InvalidParent").</summary>
    public static readonly DomainError InvalidParent = new(
        "Office.InvalidParent",
        "The parent office id is not valid.");

    /// <summary>Another office already uses the code ("Office.DuplicateCode").</summary>
    public static readonly DomainError DuplicateCode = new(
        "Office.DuplicateCode",
        "An office with this code already exists.");

    /// <summary>An office would become its own parent ("Office.CannotBeOwnParent").</summary>
    public static readonly DomainError CannotBeOwnParent = new(
        "Office.CannotBeOwnParent",
        "An office cannot be its own parent.");

    /// <summary>An office would be moved under its own descendant ("Office.CannotMoveUnderDescendant").</summary>
    public static readonly DomainError CannotMoveUnderDescendant = new(
        "Office.CannotMoveUnderDescendant",
        "An office cannot be moved under one of its own descendants.");

    /// <summary>The hierarchy would exceed 4 levels ("Office.MaxDepthExceeded").</summary>
    public static readonly DomainError MaxDepthExceeded = new(
        "Office.MaxDepthExceeded",
        $"The office hierarchy may be at most {AssetLite.Domain.Offices.Office.MaxHierarchyDepth} levels deep (HQ → region → site → room).");
}
