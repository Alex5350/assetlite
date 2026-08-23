using AssetLite.Domain.Assets;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Asset"/> aggregate (including its assignment history).</summary>
public interface IAssetRepository
{
    /// <summary>Loads a single aggregate (with assignment history) by id.</summary>
    /// <param name="id">The asset id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The asset, or <see langword="null"/> when not found.</returns>
    Task<Asset?> GetByIdAsync(AssetId id, CancellationToken cancellationToken = default);

    /// <summary>Loads a single aggregate (with assignment history) by its unique tag.</summary>
    /// <param name="tag">The asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The asset, or <see langword="null"/> when not found.</returns>
    Task<Asset?> GetByTagAsync(AssetTag tag, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a tag is already in use (concurrency backstop for tag allocation).</summary>
    /// <param name="tag">The asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the tag exists.</returns>
    Task<bool> TagExistsAsync(AssetTag tag, CancellationToken cancellationToken = default);

    /// <summary>Stages a new asset for insertion.</summary>
    /// <param name="asset">The asset to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);

    /// <summary>Stages changes to an existing asset (state, office, assignment history).</summary>
    /// <param name="asset">The asset to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches assets by filter. <see cref="AssetSearchFilter.SearchText"/> matches name,
    /// serial number, tag and model with "contains" semantics (case-insensitive). Results are
    /// paged and ordered deterministically (by tag number) so pages stay stable.
    /// </summary>
    /// <param name="filter">The filter and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching page plus the total number of matches.</returns>
    Task<(IReadOnlyList<Asset> Items, int Total)> SearchAsync(AssetSearchFilter filter, CancellationToken cancellationToken = default);
}
