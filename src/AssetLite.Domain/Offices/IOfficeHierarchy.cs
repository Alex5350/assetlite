using AssetLite.Domain.Common;
using AssetLite.Domain.Identities;

namespace AssetLite.Domain.Offices;

/// <summary>
/// Domain service contract for office hierarchy rules that require access to persisted state.
/// Declared in the Domain layer; implemented in the Application layer on top of repository ports.
/// </summary>
/// <remarks>
/// Implementations enforce: no office becomes its own parent, no office is re-parented under
/// its own descendant (acyclicity), and the hierarchy never exceeds
/// <see cref="Office.MaxHierarchyDepth"/> levels (HQ → region → site → room).
/// </remarks>
public interface IOfficeHierarchy
{
    /// <summary>
    /// Determines whether <paramref name="candidateDescendantId"/> is a strict descendant of
    /// <paramref name="ancestorId"/> by walking the parent chain of the candidate. An office is
    /// not considered a descendant of itself.
    /// </summary>
    /// <param name="candidateDescendantId">The office whose ancestry is checked.</param>
    /// <param name="ancestorId">The suspected ancestor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a strict descendant relationship exists.</returns>
    Task<bool> IsDescendantOfAsync(
        OfficeId candidateDescendantId,
        OfficeId ancestorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that <paramref name="newParentOfficeId"/> is an acceptable parent for
    /// <paramref name="childId"/>: the parent exists, is not the child itself, is not a
    /// descendant of the child, and the resulting depth stays within
    /// <see cref="Office.MaxHierarchyDepth"/>.
    /// </summary>
    /// <param name="childId">The office being created or moved.</param>
    /// <param name="newParentOfficeId">
    /// The intended parent, or <see langword="null"/> when placing the office as root (the
    /// single-root invariant is enforced by Application features, not by this check).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful result, or <see cref="Errors.OfficeErrors.CannotBeOwnParent"/>,
    /// <see cref="Errors.OfficeErrors.NotFound"/>,
    /// <see cref="Errors.OfficeErrors.CannotMoveUnderDescendant"/> or
    /// <see cref="Errors.OfficeErrors.MaxDepthExceeded"/>.
    /// </returns>
    Task<DomainResult> EnsureValidParentAsync(
        OfficeId childId,
        OfficeId? newParentOfficeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Collects the office identified by <paramref name="rootId"/> together with all of its
    /// descendants (breadth-first). Returns an empty list when the root does not exist.
    /// </summary>
    /// <param name="rootId">The hierarchy subtree root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root id followed by all descendant ids.</returns>
    Task<IReadOnlyList<OfficeId>> CollectOfficeAndDescendantsAsync(
        OfficeId rootId,
        CancellationToken cancellationToken = default);
}
