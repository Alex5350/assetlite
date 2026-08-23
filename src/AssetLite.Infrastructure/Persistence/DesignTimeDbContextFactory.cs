using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet-ef</c> can create the context without a running host. Uses the
/// same default connection string as <see cref="DependencyInjection.AddInfrastructure"/>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssetLiteDbContext>
{
    /// <summary>Default SQLite connection used when <c>ConnectionStrings:AssetLite</c> is absent.</summary>
    public const string DefaultConnectionString = "Data Source=assetlite.db";

    /// <inheritdoc />
    public AssetLiteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AssetLiteDbContext>()
            .UseSqlite(DefaultConnectionString)
            .Options;
        return new AssetLiteDbContext(options);
    }
}
