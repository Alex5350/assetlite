using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Read-only office endpoints against the seeded database.</summary>
public sealed class OfficesApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;

    public OfficesApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    internal static async Task<JsonDocument> GetTreeAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/offices/tree", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
    }

    [Fact]
    public async Task GetTree_ReturnsSeededHierarchyWithHqAndRegions()
    {
        var tree = await GetTreeAsync(_client);

        Assert.Equal("Headquarters", tree.GetString("name"));
        Assert.Equal("ASTHQ", tree.GetString("code"));
        var children = tree.EnumerateArray("children").ToList();
        Assert.Equal(["East Region", "West Region"], children.Select(child => child.GetProperty("name").GetString()));
        var east = children[0];
        var eastChildren = east.GetProperty("children").EnumerateArray().ToList();
        Assert.Equal(
            ["Boston Site", "New York Site"],
            eastChildren.Select(child => child.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task GetTree_ContainsAllSevenSeededOffices()
    {
        var tree = await GetTreeAsync(_client);

        var names = new List<string>();
        CollectNames(tree.RootElement, names);
        Assert.Equal(7, names.Count);
        Assert.Contains("Los Angeles Site", names);
        Assert.Contains("San Francisco Site", names);
        return;

        static void CollectNames(JsonElement node, List<string> into)
        {
            into.Add(node.GetProperty("name").GetString()!);
            foreach (var child in node.GetProperty("children").EnumerateArray())
            {
                CollectNames(child, into);
            }
        }
    }

    [Fact]
    public async Task GetList_ReturnsAllSeededOfficesOrderedByName()
    {
        var response = await _client.GetAsync("/api/offices", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offices = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal(7, offices.RootElement.GetArrayLength());
        var names = offices.RootElement.EnumerateArray().Select(office => office.GetProperty("name").GetString()).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
    }
}

/// <summary>Office mutations (create / move) against a dedicated seeded database.</summary>
public sealed class OfficesMutationApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public OfficesMutationApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithParent_Returns201WithLocationAndBody()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");

        var response = await _client.PostAsJsonAsync(
            "/api/offices",
            new { name = "Chicago Site", code = "ASTCHI", parentOfficeId = hqId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("api/offices", response.Headers.Location.ToString());

        var created = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Chicago Site", created.GetString("name"));
        Assert.Equal("ASTCHI", created.GetString("code"));
        Assert.Equal(hqId, created.GetGuid("parentOfficeId"));

        // The new office is a direct HQ child immediately afterwards.
        var updated = await OfficesApiTests.GetTreeAsync(_client);
        Assert.Contains(
            updated.EnumerateArray("children"),
            child => child.GetProperty("name").GetString() == "Chicago Site");
    }

    [Fact]
    public async Task Create_WithDuplicateCode_Returns409WithDuplicateCodeTitle()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");

        var response = await _client.PostAsJsonAsync(
            "/api/offices",
            new { name = "Imposter HQ", code = "ASTHQ", parentOfficeId = hqId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Office.DuplicateCode", problem.GetString("title"));
    }

    [Fact]
    public async Task Create_WithoutParentWhenRootExists_Returns409()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/offices",
            new { name = "Second HQ", code = "ASTHQ2" },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Office.RootAlreadyExists", problem.GetString("title"));
    }

    [Fact]
    public async Task Create_WithInvalidCode_Returns400ValidationProblem()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");

        var response = await _client.PostAsJsonAsync(
            "/api/offices",
            new { name = "Bad Code Office", code = "bad-code", parentOfficeId = hqId },
            _json,
            TestContext.Current.CancellationToken);

        // The FluentValidation boundary rule (uppercase alphanumeric, 3-8 chars) rejects the
        // request before the domain shape check runs.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.True(problem.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Code", out _), "Expected a 'Code' entry in the errors dictionary.");
    }

    [Fact]
    public async Task Move_UnderOwnDescendant_Returns409WithCannotMoveUnderDescendant()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");
        var eastId = tree.EnumerateArray("children")
            .First(child => child.GetProperty("name").GetString() == "East Region")
            .GetProperty("id").GetGuid();

        var response = await _client.PostAsJsonAsync(
            $"/api/offices/{hqId}/move",
            new { newParentOfficeId = eastId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Office.CannotMoveUnderDescendant", problem.GetString("title"));
    }

    [Fact]
    public async Task Move_ToOtherBranch_Returns204AndReparents()
    {
        var tree = await OfficesApiTests.GetTreeAsync(_client);
        var hqId = tree.GetGuid("id");
        var westId = tree.EnumerateArray("children")
            .First(child => child.GetProperty("name").GetString() == "West Region")
            .GetProperty("id").GetGuid();

        // Create a fresh child under HQ, then move it under West.
        var created = await _client.PostAsJsonAsync(
            "/api/offices",
            new { name = "Chicago Site", code = "ASTMVE", parentOfficeId = hqId },
            _json,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var chicagoId = (await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson().GetGuid("id");

        var response = await _client.PostAsJsonAsync(
            $"/api/offices/{chicagoId}/move",
            new { newParentOfficeId = westId },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await OfficesApiTests.GetTreeAsync(_client);
        var west = updated.EnumerateArray("children")
            .First(child => child.GetProperty("name").GetString() == "West Region");
        Assert.Contains(
            west.GetProperty("children").EnumerateArray(),
            child => child.GetProperty("name").GetString() == "Chicago Site");
    }

    [Fact]
    public async Task Move_UnknownOffice_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/offices/{Guid.NewGuid()}/move",
            new { newParentOfficeId = Guid.NewGuid() },
            _json,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();
        Assert.Equal("Office.NotFound", problem.GetString("title"));
    }
}
