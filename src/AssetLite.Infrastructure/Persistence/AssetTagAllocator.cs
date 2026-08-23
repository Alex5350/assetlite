using AssetLite.Application.Abstractions;
using AssetLite.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Sequential tag allocator backed by a MAX-style query: the next tag is
/// <c>MAX(stored tag) + 1</c>. Because tags are stored canonically zero-padded, ordering by the
/// column is numeric, and the unique index on the Tag column is the final concurrency backstop.
/// </summary>
internal sealed class AssetTagAllocator(AssetLiteDbContext dbContext) : IAssetTagAllocator
{
    /// <inheritdoc />
    public async Task<AssetTag> AllocateAsync(CancellationToken cancellationToken = default)
    {
        var highestTag = await dbContext.Assets
            .AsNoTracking()
            .OrderByDescending(asset => asset.Tag)
            .Select(asset => (AssetTag?)asset.Tag)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = (highestTag?.Number ?? 0) + 1;
        if (nextNumber > AssetTag.MaxNumber)
        {
            throw new InvalidOperationException(
                $"Asset tag sequence exhausted: no numbers left below {AssetTag.MaxNumber}.");
        }

        return AssetTag.FromNumber(nextNumber).GetValueOrThrow();
    }
}
