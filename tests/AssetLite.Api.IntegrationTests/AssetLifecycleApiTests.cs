using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>
/// Full asset lifecycle over HTTP against a dedicated seeded database: register, assign, return,
/// maintenance, resume, retire, dispose — including the conflict mappings on invalid transitions.
/// </summary>
public sealed class AssetLifecycleApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AssetLifecycleApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    private async Task<(Guid CategoryId, Guid OfficeId)> GetReferenceIdsAsync()
    {
        var categories = await _client.GetFromJsonAsync<JsonElement>("/api/categories", _json, TestContext.Current.CancellationToken);
        var categoryId = categories.EnumerateArray()
            .Single(category => category.GetProperty("name").GetString() == "Laptops")
            .GetProperty("id").GetGuid();
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        return (categoryId, tree.GetGuid("id"));
    }

    private async Task<JsonDocument> GetAssetAsync(string tag)
    {
        var response = await _client.GetAsync($"/api/assets/{tag}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
    }

    [Fact]
    public async Task FullLifecycle_FromRegistrationToDisposal_SucceedsWithMappedStatusCodes()
    {
        var (categoryId, officeId) = await GetReferenceIdsAsync();

        // Register: 201 + Location + allocated tag in the body (45 seeded assets -> AST-000046).
        var created = await _client.PostAsJsonAsync(
            "/api/assets",
            new
            {
                categoryId,
                officeId,
                name = "Integration Test Laptop",
                condition = "New",
                manufacturer = "TestCo",
                model = "TestBook 1",
                serialNumber = "TEST-000001",
                purchaseCost = 999.99m,
                currency = "USD",
            },
            _json,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var createdBody = (await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        var tag = createdBody.GetString("tag");
        Assert.Equal("AST-000046", tag);
        Assert.Contains(tag, created.Headers.Location.ToString());
        Assert.Equal("InStock", createdBody.GetString("status"));
        Assert.Equal("Headquarters", createdBody.GetString("officeName"));

        // Assign: 204, then the detail shows the current assignee.
        var assignResponse = await _client.PostAsJsonAsync(
            $"/api/assets/{tag}/assign",
            new { assigneeName = "Test User", assigneeEmail = "test.user@assetlite.example" },
            _json,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);
        var afterAssign = await GetAssetAsync(tag);
        Assert.Equal("Assigned", afterAssign.GetString("status"));
        Assert.Equal("Test User", afterAssign.GetString("currentAssigneeName"));

        // Return: 204, back to stock with the assignment closed.
        var returnResponse = await _client.PostAsync($"/api/assets/{tag}/return", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);
        var afterReturn = await GetAssetAsync(tag);
        Assert.Equal("InStock", afterReturn.GetString("status"));
        Assert.Null(afterReturn.Property("currentAssigneeName").GetString());
        Assert.NotNull(afterReturn.Property("assignments").EnumerateArray().Single().GetProperty("returnedAtUtc").GetString());

        // Maintenance -> resume: both 204.
        var maintenanceResponse = await _client.PostAsync($"/api/assets/{tag}/maintenance", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, maintenanceResponse.StatusCode);
        var afterMaintenance = await GetAssetAsync(tag);
        Assert.Equal("Maintenance", afterMaintenance.GetString("status"));

        var resumeResponse = await _client.PostAsync($"/api/assets/{tag}/maintenance/resume", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, resumeResponse.StatusCode);
        Assert.Equal("InStock", (await GetAssetAsync(tag)).GetString("status"));

        // Retire -> dispose: both 204, ending in the terminal state.
        var retireResponse = await _client.PostAsync($"/api/assets/{tag}/retire", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, retireResponse.StatusCode);
        Assert.Equal("Retired", (await GetAssetAsync(tag)).GetString("status"));

        var disposeResponse = await _client.PostAsync($"/api/assets/{tag}/dispose", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, disposeResponse.StatusCode);
        Assert.Equal("Disposed", (await GetAssetAsync(tag)).GetString("status"));

        // Disposing again (or any invalid transition) yields a 409 problem with an "Asset." code.
        var disposeAgainResponse = await _client.PostAsync($"/api/assets/{tag}/dispose", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, disposeAgainResponse.StatusCode);
        var problem = (await disposeAgainResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.NotRetired", problem.GetString("title"));
        Assert.StartsWith("Asset.", problem.GetString("title"));
    }

    [Fact]
    public async Task Assign_WithDisposedSeededAsset_Returns409WithTypedAssetCode()
    {
        // AST-000012 is seeded as Retired -> Disposed.
        var response = await _client.PostAsJsonAsync(
            "/api/assets/AST-000012/assign",
            new { assigneeName = "Test User", assigneeEmail = "test.user@assetlite.example" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.StartsWith("Asset.", problem.GetString("title"));
    }

    [Fact]
    public async Task Return_WithInStockSeededAsset_Returns409()
    {
        var response = await _client.PostAsync("/api/assets/AST-000003/return", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.NotAssigned", problem.GetString("title"));
    }

    [Fact]
    public async Task Retire_WithAlreadyRetiredSeededAsset_Returns409()
    {
        var response = await _client.PostAsync("/api/assets/AST-000008/retire", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.AlreadyRetired", problem.GetString("title"));
    }

    [Fact]
    public async Task Transfer_ToSameOffice_Returns409()
    {
        // Resolve the asset's current office so the test holds regardless of other transfers.
        var detail = await GetAssetAsync("AST-000003");
        var currentOfficeId = detail.Property("officeId").GetGuid();

        var response = await _client.PostAsJsonAsync(
            "/api/assets/AST-000003/transfer",
            new { targetOfficeId = currentOfficeId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Asset.AlreadyInTargetOffice", problem.GetString("title"));
    }

    [Fact]
    public async Task Transfer_ToOtherOffice_Returns204AndMovesTheAsset()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var bosId = tree.EnumerateArray("children")
            .SelectMany(region => region.GetProperty("children").EnumerateArray())
            .First(site => site.GetProperty("name").GetString() == "Boston Site")
            .GetProperty("id").GetGuid();

        var response = await _client.PostAsJsonAsync(
            "/api/assets/AST-000003/transfer",
            new { targetOfficeId = bosId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detail = await GetAssetAsync("AST-000003");
        Assert.Equal("Boston Site", detail.GetString("officeName"));
    }

    [Fact]
    public async Task Register_WithUnknownCategory_Returns404()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var officeId = tree.GetGuid("id");

        var response = await _client.PostAsJsonAsync(
            "/api/assets",
            new { categoryId = Guid.NewGuid(), officeId, name = "Orphan Asset", condition = 1 },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Category.NotFound", problem.GetString("title"));
    }
}
