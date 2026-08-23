using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;

namespace AssetLite.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Office"/> aggregate.</summary>
/// <remarks>
/// Implementations must fully load the office (identity + fields); hierarchy traversal uses
/// these members, so callers rely on <see cref="GetByIdAsync"/> returning parents promptly.
/// </remarks>
public interface IOfficeRepository
{
    /// <summary>Loads a single office by id.</summary>
    /// <param name="id">The office id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The office, or <see langword="null"/> when not found.</returns>
    Task<Office?> GetByIdAsync(OfficeId id, CancellationToken cancellationToken = default);

    /// <summary>Loads the root (HQ) office — the office whose parent is null.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root office, or <see langword="null"/> when none exists.</returns>
    Task<Office?> GetRootAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the direct children of an office.</summary>
    /// <param name="parentId">The parent office id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The direct child offices.</returns>
    Task<IReadOnlyList<Office>> ListChildrenAsync(OfficeId parentId, CancellationToken cancellationToken = default);

    /// <summary>Loads every office in the organization.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All offices.</returns>
    Task<IReadOnlyList<Office>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Determines whether an office has child offices.</summary>
    /// <param name="id">The office id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the office has children.</returns>
    Task<bool> HasChildrenAsync(OfficeId id, CancellationToken cancellationToken = default);

    /// <summary>Determines whether any asset is located in the office.</summary>
    /// <param name="id">The office id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the office holds assets.</returns>
    Task<bool> HasAssetsAsync(OfficeId id, CancellationToken cancellationToken = default);

    /// <summary>Determines whether an office with the given code already exists.</summary>
    /// <param name="code">The office code (normalized uppercase).</param>
    /// <param name="excludeOfficeId">Optional office id to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the code is taken.</returns>
    Task<bool> CodeExistsAsync(string code, OfficeId? excludeOfficeId = null, CancellationToken cancellationToken = default);

    /// <summary>Stages a new office for insertion.</summary>
    /// <param name="office">The office to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAsync(Office office, CancellationToken cancellationToken = default);

    /// <summary>Stages changes to an existing office.</summary>
    /// <param name="office">The office to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateAsync(Office office, CancellationToken cancellationToken = default);
}
