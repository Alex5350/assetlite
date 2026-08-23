using AssetLite.Application.Abstractions;
using ErrorOr;

namespace AssetLite.Application.Offices;

/// <summary>Returns all offices as a flat list ordered by name.</summary>
public sealed record ListOfficesQuery : IQuery<IReadOnlyList<OfficeDto>>;

/// <summary>Handles <see cref="ListOfficesQuery"/>.</summary>
/// <param name="OfficeRepository">Office repository port.</param>
public sealed class ListOfficesHandler(IOfficeRepository OfficeRepository)
    : IQueryHandler<ListOfficesQuery, IReadOnlyList<OfficeDto>>
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<OfficeDto>>> HandleAsync(ListOfficesQuery query, CancellationToken cancellationToken = default)
    {
        var offices = await OfficeRepository.ListAllAsync(cancellationToken);
        return offices.OrderBy(office => office.Name).Select(office => office.ToDto()).ToList();
    }
}
