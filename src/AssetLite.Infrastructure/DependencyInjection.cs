using AssetLite.Application.Abstractions;
using AssetLite.Infrastructure.Labels;
using AssetLite.Infrastructure.Messaging;
using AssetLite.Infrastructure.Persistence;
using AssetLite.Infrastructure.Persistence.Repositories;
using AssetLite.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetLite.Infrastructure;

/// <summary>
/// DI entry point for the Infrastructure layer. Call <c>services.AddInfrastructure(configuration)</c>
/// from the host after <c>AddApplication()</c>.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
/// <item><c>ConnectionStrings:AssetLite</c> — SQLite connection string (default <c>Data Source=assetlite.db</c>).</item>
/// <item><c>AssetLabels:PublicBaseUrl</c> — origin used for QR payloads (default <c>http://localhost:5070</c>).</item>
/// </list>
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Registers the DbContext, all Application ports and the label/export services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (queried lazily inside the DbContext lambda).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // The options lambda runs per scope build, so the connection string is resolved lazily
        // (Aspire/host services may rewrite configuration during startup before any scope exists).
        services.AddDbContext<AssetLiteDbContext>((provider, options) =>
        {
            var resolved = provider.GetRequiredService<IConfiguration>();
            var connectionString = resolved.GetConnectionString("AssetLite")
                ?? DesignTimeDbContextFactory.DefaultConnectionString;
            options.UseSqlite(connectionString);
        });

        // Persistence ports.
        services.AddScoped<IOfficeRepository, OfficeRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAssetTagAllocator, AssetTagAllocator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseInitializer>();

        // Cross-cutting ports.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        // Labeling and exports (consumed by Api/frontend; interfaces live in Infrastructure).
        services.AddSingleton<IAssetLabelService, AssetLabelService>();
        services.AddScoped<IReportExportService, ReportExportService>();

        return services;
    }
}
