using AssetLite.Domain.ValueObjects;

namespace AssetLite.Application.Abstractions;

/// <summary>
/// Allocates unique, strictly sequential asset tags: AST-000001, AST-000002, ...
/// </summary>
/// <remarks>
/// Design: the implementation (Infrastructure) derives the next number deterministically from
/// persisted state — for example <c>MAX(tag_number) + 1</c> evaluated inside the same unit of
/// work as the insert — and a unique index on the tag column is the final concurrency backstop.
/// Tests replace this port with an in-memory counter.
/// </remarks>
public interface IAssetTagAllocator
{
    /// <summary>Returns the next unused asset tag.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next tag in the sequence.</returns>
    Task<AssetTag> AllocateAsync(CancellationToken cancellationToken = default);
}
