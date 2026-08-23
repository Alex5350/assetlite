using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssetLite.Api.IntegrationTests;

/// <summary>
/// Hosts the real API in the Development environment against a unique throwaway SQLite file per
/// test class. Development mode runs <c>DatabaseInitializer.InitializeDevelopmentAsync</c>, so the
/// host starts with the full demo catalog (7 offices, 7 categories, 45 assets).
/// </summary>
public sealed class AssetLiteApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"assetlite-tests-{Guid.NewGuid():N}.db");

    private bool _databaseDeleted;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:AssetLite", $"Data Source={_databasePath}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteDatabaseFiles();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            DeleteDatabaseFiles();
        }
    }

    private void DeleteDatabaseFiles()
    {
        if (_databaseDeleted)
        {
            return;
        }

        _databaseDeleted = true;
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            try
            {
                var path = _databasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Best effort: the temp directory cleans up eventually.
            }
        }
    }
}
