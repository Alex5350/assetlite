using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Common;
using AssetLite.Domain.Offices;
using AssetLite.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and (Development only) seeds the demo catalog. Seeding is idempotent: each
/// section only runs when its table is empty, so restarts and parallel boots are safe.
/// </summary>
/// <remarks>
/// Seeded aggregates are built through the domain factories and lifecycle methods, so the sample
/// data always satisfies the aggregate invariants. Buffered domain events raised while replaying
/// histories are pulled and discarded (they describe long-past transitions).
/// </remarks>
public sealed class DatabaseInitializer(AssetLiteDbContext dbContext, ILogger<DatabaseInitializer> logger)
{
    /// <summary>Applies pending migrations.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }

    /// <summary>Applies pending migrations and seeds the Development sample data when empty.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task InitializeDevelopmentAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Offices.AnyAsync(cancellationToken))
        {
            dbContext.Offices.AddRange(BuildOffices());
        }

        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            dbContext.Categories.AddRange(SeedData.Categories.Select(CreateCategory));
        }

        // Assets reference offices and categories staged above; EF orders the inserts.
        if (!await dbContext.Assets.AnyAsync(cancellationToken))
        {
            var offices = dbContext.Offices.Local.ToDictionary(office => office.Code);
            var categories = dbContext.Categories.Local.ToDictionary(category => category.Name);
            dbContext.Assets.AddRange(SeedData.Assets.Select(spec => CreateAsset(spec, offices, categories)));
        }
        else
        {
            logger.LogInformation("Sample data present; seeding skipped.");
            return;
        }

        var affected = await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded sample data: {Offices} offices, {Categories} categories, {Assets} assets ({Rows} rows).",
            SeedData.Offices.Length, SeedData.Categories.Length, SeedData.Assets.Length, affected);
    }

    /// <summary>
    /// Builds the office tree in declaration order so parents exist before children; parents are
    /// linked through the in-memory ids assigned by the domain factory.
    /// </summary>
    private static IEnumerable<Office> BuildOffices()
    {
        var byCode = new Dictionary<string, Office>(StringComparer.Ordinal);
        foreach (var spec in SeedData.Offices)
        {
            Domain.Identities.OfficeId? parent = spec.ParentCode is null ? null : byCode[spec.ParentCode].Id;
            var office = Office.Create(spec.Name, spec.Code, parent).GetValueOrThrow();
            byCode[spec.Code] = office;
            yield return office;
        }
    }

    private static AssetCategory CreateCategory((string Name, string Description, int LifespanMonths) spec) =>
        AssetCategory.Create(spec.Name, spec.Description, spec.LifespanMonths).GetValueOrThrow();

    private static Asset CreateAsset(
        SeedAsset spec,
        IReadOnlyDictionary<string, Office> offices,
        IReadOnlyDictionary<string, AssetCategory> categories)
    {
        var office = offices[spec.OfficeCode];
        var category = categories[spec.CategoryName];
        var cost = Money.Create(spec.PurchaseCost, Money.DefaultCurrency).GetValueOrThrow();
        var createdAt = new DateTimeOffset(spec.PurchaseDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        var result = Asset.Create(
            AssetTag.FromNumber(spec.TagNumber).GetValueOrThrow(),
            category.Id,
            office.Id,
            spec.Name,
            spec.Condition,
            createdAt,
            spec.Manufacturer,
            spec.Model,
            spec.SerialNumber,
            spec.PurchaseDate,
            cost,
            spec.Notes);
        var asset = result.GetValueOrThrow();

        foreach (var assignment in spec.History)
        {
            asset.AssignTo(assignment.AssigneeName, assignment.AssigneeEmail, assignment.AssignedAtUtc).EnsureSuccess();
            if (assignment.ReturnedAtUtc is { } returnedAt)
            {
                asset.ReturnToStock(returnedAt).EnsureSuccess();
            }
        }

        var asOf = spec.StateChangedAtUtc ?? createdAt.AddDays(90);
        switch (spec.FinalState)
        {
            case SeedFinalState.Maintenance:
                asset.StartMaintenance(asOf).EnsureSuccess();
                break;
            case SeedFinalState.Retired:
                asset.Retire(asOf).EnsureSuccess();
                break;
            case SeedFinalState.Disposed:
                asset.Retire(asOf).EnsureSuccess();
                asset.Dispose().EnsureSuccess();
                break;
        }

        asset.PullEvents(); // past transitions are history, not events to dispatch
        return asset;
    }
}

/// <summary>Assertion helper for seed replay: seed data is static and must always be valid.</summary>
internal static class DomainResultExtensions
{
    /// <summary>Throws when the result is a failure (seed data bug).</summary>
    /// <param name="result">The result to assert.</param>
    public static void EnsureSuccess(this DomainResult result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Seed replay failed: {result.Error!.Code} {result.Error.Message}");
        }
    }
}
