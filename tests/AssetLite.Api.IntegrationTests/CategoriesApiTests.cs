using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Category endpoints: list, create/update roundtrip and duplicate-name conflicts.</summary>
public sealed class CategoriesApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CategoriesApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetList_ReturnsAllSeededCategories()
    {
        var response = await _client.GetAsync("/api/categories", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(categories.RootElement.GetArrayLength() >= 7, "Expected the seven seeded categories.");
        var names = categories.RootElement.EnumerateArray()
            .Select(category => category.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("Laptops", names);
        Assert.Contains("Networking", names);
        Assert.Equal(names.Order(StringComparer.Ordinal), names); // ordered by name
    }

    [Fact]
    public async Task Create_ThenUpdate_RoundTripsTheCategory()
    {
        var created = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Test Category", description = "Created by integration tests.", expectedLifespanMonths = 24 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = (await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Test Category", createdBody.GetString("name"));
        Assert.Equal(24, createdBody.GetInt32("expectedLifespanMonths"));
        var id = createdBody.GetGuid("id");

        var updated = await _client.PutAsJsonAsync(
            $"/api/categories/{id}",
            new { name = "Renamed Category", description = "Updated by integration tests.", expectedLifespanMonths = 60 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = (await updated.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal(id, updatedBody.GetGuid("id"));
        Assert.Equal("Renamed Category", updatedBody.GetString("name"));
        Assert.Equal("Updated by integration tests.", updatedBody.GetString("description"));
        Assert.Equal(60, updatedBody.GetInt32("expectedLifespanMonths"));
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409WithDuplicateNameTitle()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Laptops", description = null as string, expectedLifespanMonths = 36 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Category.DuplicateName", problem.GetString("title"));
    }

    [Fact]
    public async Task Create_WithNonPositiveLifespan_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Bad Lifespan", description = (string?)null, expectedLifespanMonths = 0 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(problem.RootElement.TryGetProperty("errors", out _), "Expected a validation problem with an errors dictionary.");
    }

    [Fact]
    public async Task Update_UnknownCategory_Returns404()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{Guid.NewGuid()}",
            new { name = "Missing", description = (string?)null, expectedLifespanMonths = 12 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Category.NotFound", problem.GetString("title"));
    }
}
