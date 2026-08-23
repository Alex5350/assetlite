using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>FluentValidation boundary behavior: invalid request bodies become 400 validation problems.</summary>
public sealed class ValidationApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ValidationApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task RegisterAsset_WithEmptyName_Returns400WithErrorsDictionary()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/assets",
            new
            {
                categoryId = Guid.NewGuid(),
                officeId = Guid.NewGuid(),
                name = "",
                condition = 1,
            },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();

        Assert.True(problem.RootElement.TryGetProperty("errors", out var errors), "Expected an errors dictionary.");
        Assert.True(errors.ValueKind == JsonValueKind.Object, "The errors extension must be an object keyed by property.");
        Assert.True(errors.TryGetProperty("Name", out var nameErrors), "Expected a 'Name' entry in the errors dictionary.");
        Assert.True(nameErrors.ValueKind == JsonValueKind.Array);
        Assert.True(nameErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task RegisterAsset_WithNegativeCost_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/assets",
            new
            {
                categoryId = Guid.NewGuid(),
                officeId = Guid.NewGuid(),
                name = "Negative Cost Asset",
                condition = 1,
                purchaseCost = -10m,
            },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task AssignAsset_WithInvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/assets/AST-000001/assign",
            new { assigneeName = "Test User", assigneeEmail = "not-an-email" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(problem.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("AssigneeEmail", out _));
    }

    [Fact]
    public async Task SearchAssets_WithInvalidPaging_Returns400()
    {
        var response = await _client.GetAsync("/api/assets?page=0&pageSize=500", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
