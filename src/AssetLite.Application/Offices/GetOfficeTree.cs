using AssetLite.Application.Abstractions;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Offices;
using ErrorOr;

namespace AssetLite.Application.Offices;

/// <summary>Returns the full office tree, rooted at the HQ office.</summary>
public sealed record GetOfficeTreeQuery : IQuery<OfficeTreeNodeDto>;

/// <summary>Handles <see cref="GetOfficeTreeQuery"/>.</summary>
/// <param name="OfficeRepository">Office repository port.</param>
public sealed class GetOfficeTreeHandler(IOfficeRepository OfficeRepository)
    : IQueryHandler<GetOfficeTreeQuery, OfficeTreeNodeDto>
{
    /// <inheritdoc />
    public async Task<ErrorOr<OfficeTreeNodeDto>> HandleAsync(GetOfficeTreeQuery query, CancellationToken cancellationToken = default)
    {
        var offices = await OfficeRepository.ListAllAsync(cancellationToken);
        if (offices.Count == 0)
        {
            return OfficeErrors.RootNotFound.ToError();
        }

        // Children are keyed by their (non-null) parent id; roots are identified separately.
        var childrenByParent = offices
            .Where(office => office.ParentOfficeId is not null)
            .GroupBy(office => office.ParentOfficeId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(office => office.Name).ToList());

        var roots = offices.Where(office => office.ParentOfficeId is null).ToList();
        if (roots.Count != 1)
        {
            return OfficeErrors.RootNotFound.ToError();
        }

        OfficeTreeNodeDto BuildNode(Office office) => new(
            office.Id,
            office.Name,
            office.Code,
            office.ParentOfficeId,
            (childrenByParent.GetValueOrDefault(office.Id) ?? []).Select(BuildNode).ToList());

        return BuildNode(roots[0]);
    }
}
