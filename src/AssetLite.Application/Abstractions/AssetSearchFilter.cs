using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;

namespace AssetLite.Application.Abstractions;

/// <summary>
/// Filter and pagination parameters for <c>IAssetRepository.SearchAsync</c>. All criteria are
/// optional and combined with AND semantics.
/// </summary>
/// <param name="OfficeId">Exact office filter; ignored when <paramref name="OfficeIdsIncludingDescendants"/> is set.</param>
/// <param name="OfficeIdsIncludingDescendants">Inclusive list of office ids (office plus all descendants); takes precedence over <paramref name="OfficeId"/>.</param>
/// <param name="CategoryId">Exact category filter.</param>
/// <param name="Status">Exact status filter.</param>
/// <param name="SearchText">Case-insensitive "contains" over name, serial number, tag and model.</param>
/// <param name="Page">1-based page number (defaults to 1).</param>
/// <param name="PageSize">Page size between 1 and 100 (defaults to 20); use <c>int.MaxValue</c> for full exports.</param>
public sealed record AssetSearchFilter(
    OfficeId? OfficeId = null,
    IReadOnlyList<OfficeId>? OfficeIdsIncludingDescendants = null,
    CategoryId? CategoryId = null,
    AssetStatus? Status = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 20);
