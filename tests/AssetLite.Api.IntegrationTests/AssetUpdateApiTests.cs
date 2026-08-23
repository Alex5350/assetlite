using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>
/// PUT /api/assets/{tag} against a dedicated seeded database: replacing descriptive details,
/// the immutability of disposed assets and the 400/404/409 problem mappings.
/// </summary>
public sealed class AssetUpdateApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AssetUpdateApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    private async Task<Guid> GetCategoryIdAsync(string name)
    {
        var categories = await _client.GetFromJsonAsync<JsonElement>("/api/categories", _json, TestContext.Current.CancellationToken);
        return categories.EnumerateArray()
            .Single(category => category.GetProperty("name").GetString() == name)
            .GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Update_SeededAsset_PersistsAndReturnsNewDetails()
    {
        var monitorsId = await GetCategoryIdAsync("Monitors");

        var response = await _client.PutAsJsonAsync(
            "/api/assets/AST-000001",
            new
            {
                categoryId = monitorsId,
                name = "ThinkPad T14s Gen 6 (refurb)",
                condition = "Fair",
                manufacturer = "Lenovo",
                model = "T14s Gen 6",
                serialNumber = "PF3RT6LN",
                purchaseDate = new DateOnly(2024, 3, 15),
                purchaseCost = 1299.50m,
                currency = "USD",
                notes = "Display replaced under warranty.",
            },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("AST-000001", body.GetString("tag"));
        Assert.Equal("ThinkPad T14s Gen 6 (refurb)", body.GetString("name"));
        Assert.Equal("Fair", body.GetString("condition"));
        Assert.Equal("Monitors", body.GetString("categoryName"));
        Assert.Equal(1299.50m, body.Property("purchaseCostAmount").GetDecimal());

        // The change survives a round-trip through the store.
        var reloaded = await _client.GetFromJsonAsync<JsonElement>("/api/assets/AST-000001", _json, TestContext.Current.CancellationToken);
        Assert.Equal("ThinkPad T14s Gen 6 (refurb)", reloaded.GetProperty("name").GetString());
        Assert.Equal(monitorsId, reloaded.GetProperty("categoryId").GetGuid());
    }

    [Fact]
    public async Task Update_UnknownTag_Returns404()
    {
        var categoryId = await GetCategoryIdAsync("Laptops");

        var response = await _client.PutAsJsonAsync(
            "/api/assets/AST-999999",
            new { categoryId, name = "Ghost laptop", condition = "New" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownCategory_Returns404()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/assets/AST-000001",
            new { categoryId = Guid.NewGuid(), name = "Orphaned laptop", condition = "New" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Category.NotFound", problem.GetString("title"));
    }

    [Fact]
    public async Task Update_DisposedAsset_Returns409WithTypedAssetCode()
    {
        // AST-000012 is seeded as Retired -> Disposed.
        var categoryId = await GetCategoryIdAsync("Laptops");

        var response = await _client.PutAsJsonAsync(
            "/api/assets/AST-000012",
            new { categoryId, name = "Should not change", condition = "New" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.CannotUpdateDisposed", problem.GetString("title"));

        var reloaded = await _client.GetFromJsonAsync<JsonElement>("/api/assets/AST-000012", _json, TestContext.Current.CancellationToken);
        Assert.NotEqual("Should not change", reloaded.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Update_WithInvalidBody_Returns400ValidationProblem()
    {
        var categoryId = await GetCategoryIdAsync("Laptops");

        var response = await _client.PutAsJsonAsync(
            "/api/assets/AST-000001",
            new { categoryId, name = "", condition = "New" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("One or more validation errors occurred.", problem.GetString("title"));
    }
}
