using System.Net;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>API surface contract tests: every route is present in the OpenAPI document, /health works.</summary>
public sealed class ApiSurfaceTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;

    public ApiSurfaceTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task OpenApiDocument_ContainsEveryApiRoute()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.ValueKind == JsonValueKind.Object, "The OpenAPI document exposes a paths object.");

        foreach (var path in AllApiRoutes)
        {
            Assert.True(
                paths.TryGetProperty(path, out _),
                $"Expected the OpenAPI document to contain the route '{path}'.");
        }
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static readonly string[] AllApiRoutes =
    [
        // Offices
        "/api/offices",
        "/api/offices/tree",
        "/api/offices/{id}/move",
        // Categories
        "/api/categories",
        "/api/categories/{id}",
        // Assets
        "/api/assets",
        "/api/assets/{tag}",
        "/api/assets/{tag}/assign",
        "/api/assets/{tag}/return",
        "/api/assets/{tag}/maintenance",
        "/api/assets/{tag}/maintenance/resume",
        "/api/assets/{tag}/retire",
        "/api/assets/{tag}/dispose",
        "/api/assets/{tag}/transfer",
        "/api/assets/{tag}/label",
        // Reports
        "/api/reports/summary",
        "/api/reports/register/excel",
        "/api/reports/register/pdf",
    ];
}
