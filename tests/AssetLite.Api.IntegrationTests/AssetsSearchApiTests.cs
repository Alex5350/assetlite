using System.Net;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Read-only asset search and detail endpoints against the seeded 45-asset catalog.</summary>
public sealed class AssetsSearchApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;

    public AssetsSearchApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    private async Task<JsonDocument> SearchAsync(string query)
    {
        var response = await _client.GetAsync($"/api/assets{query}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
    }

    [Fact]
    public async Task Search_WithMacbookTerm_FindsTheFourSeededMacBooks()
    {
        var result = await SearchAsync("?search=macbook");

        Assert.Equal(4, result.GetInt32("total"));
        var items = result.EnumerateArray("items").ToList();
        Assert.Equal(4, items.Count);
        Assert.All(
            items,
            item => Assert.Contains("macbook", item.GetProperty("name").GetString()!, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, result.GetInt32("page"));
        Assert.Equal(20, result.GetInt32("pageSize"));
    }

    [Fact]
    public async Task Search_WithPaging_ReturnsTheFirstPageOfTenOutOfFortyFive()
    {
        var result = await SearchAsync("?page=1&pageSize=10");

        Assert.Equal(45, result.GetInt32("total"));
        Assert.Equal(10, result.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(1, result.GetInt32("page"));
        Assert.Equal(10, result.GetInt32("pageSize"));
        Assert.Equal(5, result.GetInt32("totalPages"));

        var items = result.EnumerateArray("items").ToList();
        Assert.Equal("AST-000001", items[0].GetProperty("tag").GetString()); // ordered by tag
        Assert.Equal("AST-000010", items[^1].GetProperty("tag").GetString());
    }

    [Fact]
    public async Task Search_SecondPage_ContinuesTheTagSequence()
    {
        var result = await SearchAsync("?page=2&pageSize=10");

        Assert.Equal(45, result.GetInt32("total"));
        var items = result.EnumerateArray("items").ToList();
        Assert.Equal("AST-000011", items[0].GetProperty("tag").GetString());
        Assert.Equal("AST-000020", items[^1].GetProperty("tag").GetString());
    }

    [Fact]
    public async Task Search_ByHqOffice_SubtreeIncludesDescendantsOnlyWhenRequested()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");

        var directOnly = await SearchAsync($"?officeId={hqId}&includeDescendants=false");
        var withDescendants = await SearchAsync($"?officeId={hqId}&includeDescendants=true");

        var directTotal = directOnly.GetInt32("total");
        var subtreeTotal = withDescendants.GetInt32("total");

        Assert.Equal(8, directTotal);           // assets located directly in Headquarters
        Assert.Equal(45, subtreeTotal);         // the HQ subtree spans every seeded office
        Assert.True(subtreeTotal > directTotal, "The subtree search must return at least the direct matches.");
    }

    [Fact]
    public async Task Search_ByStatus_ReturnsOnlyMatchingStatuses()
    {
        var result = await SearchAsync("?status=Retired");

        Assert.True(result.GetInt32("total") > 0);
        Assert.All(
            result.EnumerateArray("items").ToList(),
            item => Assert.Equal("Retired", item.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task GetByTag_WithSeededTag_ReturnsDetailWithNamesAndHistory()
    {
        var response = await _client.GetAsync("/api/assets/AST-000001", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("AST-000001", detail.GetString("tag"));
        Assert.Equal("Dell Latitude 5540", detail.GetString("name"));
        Assert.Equal("New York Site", detail.GetString("officeName"));
        Assert.Equal("Laptops", detail.GetString("categoryName"));
        Assert.Equal("Assigned", detail.GetString("status"));
        Assert.Equal("Marcus Webb", detail.GetString("currentAssigneeName"));
        Assert.Equal(2, detail.RootElement.GetProperty("assignments").GetArrayLength()); // Sarah (closed) then Marcus (open)
    }

    [Fact]
    public async Task GetByTag_WithUnknownTag_Returns404WithAssetNotFoundTitle()
    {
        var response = await _client.GetAsync("/api/assets/AST-999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.NotFound", problem.GetString("title"));
    }

    [Fact]
    public async Task GetByTag_WithMalformedTag_Returns400ValidationProblem()
    {
        var response = await _client.GetAsync("/api/assets/AST-1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(problem.RootElement.TryGetProperty("errors", out _), "Expected a validation problem with an errors dictionary.");
    }
}
