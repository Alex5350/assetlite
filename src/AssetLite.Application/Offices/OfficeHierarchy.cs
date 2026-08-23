using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;

namespace AssetLite.Application.Offices;

/// <summary>
/// Application-layer implementation of the <see cref="IOfficeHierarchy"/> domain service, built
/// on the <see cref="Abstractions.IOfficeRepository"/> port. Enforces acyclicity, no
/// self-parenting, no re-parenting under own descendants, and the maximum depth of
/// <see cref="Office.MaxHierarchyDepth"/> (HQ → region → site → room).
/// </summary>
/// <param name="OfficeRepository">Office repository port.</param>
public sealed class OfficeHierarchy(Abstractions.IOfficeRepository OfficeRepository) : IOfficeHierarchy
{
    // Ancestor walks are bounded so corrupt data forming a cycle cannot loop forever.
    private const int MaxAncestorSteps = 64;

    /// <inheritdoc />
    public async Task<bool> IsDescendantOfAsync(
        OfficeId candidateDescendantId,
        OfficeId ancestorId,
        CancellationToken cancellationToken = default)
    {
        var currentId = candidateDescendantId;
        for (var step = 0; step < MaxAncestorSteps; step++)
        {
            var current = await OfficeRepository.GetByIdAsync(currentId, cancellationToken);
            if (current?.ParentOfficeId is not { } parentId)
            {
                return false; // reached the root without finding the ancestor
            }

            if (parentId == ancestorId)
            {
                return true;
            }

            currentId = parentId;
        }

        return true; // treat a broken (cyclic) chain as a descendant so the operation is blocked
    }

    /// <inheritdoc />
    public async Task<DomainResult> EnsureValidParentAsync(
        OfficeId childId,
        OfficeId? newParentOfficeId,
        CancellationToken cancellationToken = default)
    {
        if (newParentOfficeId is null)
        {
            // Root placement is structurally valid; the single-root invariant is enforced by
            // the CreateOffice/MoveOffice features.
            return DomainResult.Success();
        }

        if (newParentOfficeId == childId)
        {
            return DomainResult.Failure(OfficeErrors.CannotBeOwnParent);
        }

        if (await OfficeRepository.GetByIdAsync(newParentOfficeId.Value, cancellationToken) is null)
        {
            return DomainResult.Failure(OfficeErrors.NotFound);
        }

        if (await IsDescendantOfAsync(newParentOfficeId.Value, childId, cancellationToken))
        {
            return DomainResult.Failure(OfficeErrors.CannotMoveUnderDescendant);
        }

        var parentDepth = await GetDepthAsync(newParentOfficeId.Value, cancellationToken);
        if (parentDepth + 1 > Office.MaxHierarchyDepth)
        {
            return DomainResult.Failure(OfficeErrors.MaxDepthExceeded);
        }

        return DomainResult.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfficeId>> CollectOfficeAndDescendantsAsync(
        OfficeId rootId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<OfficeId>();
        if (await OfficeRepository.GetByIdAsync(rootId, cancellationToken) is null)
        {
            return result;
        }

        result.Add(rootId);
        var queue = new Queue<OfficeId>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            foreach (var child in await OfficeRepository.ListChildrenAsync(queue.Dequeue(), cancellationToken))
            {
                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private async Task<int> GetDepthAsync(OfficeId officeId, CancellationToken cancellationToken)
    {
        var depth = 1;
        var current = await OfficeRepository.GetByIdAsync(officeId, cancellationToken);
        for (var step = 0; step < MaxAncestorSteps && current?.ParentOfficeId is { } parentId; step++)
        {
            depth++;
            current = await OfficeRepository.GetByIdAsync(parentId, cancellationToken);
        }

        return depth;
    }
}
