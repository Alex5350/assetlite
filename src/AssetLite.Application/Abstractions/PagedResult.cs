namespace AssetLite.Application.Abstractions;

/// <summary>A page of results with pagination metadata.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="Total">The total number of matching items across all pages.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    /// <summary>Gets the total number of pages (0 when there are no items).</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
}
