using System.Net;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Inventory summary and register export endpoints against the seeded catalog.</summary>
public sealed class ReportsApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;

    public ReportsApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetSummary_ReturnsPerOfficeAndPerCategoryBreakdowns()
    {
        var response = await _client.GetAsync("/api/reports/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();

        Assert.Equal(45, summary.GetInt32("totalAssets"));
        Assert.Equal(7, summary.RootElement.GetProperty("offices").GetArrayLength());
        Assert.Equal(7, summary.RootElement.GetProperty("categories").GetArrayLength());

        // Every per-office count sums back to the grand total.
        var officeTotals = summary.EnumerateArray("offices")
            .Select(office => office.GetProperty("totalAssets").GetInt32())
            .ToList();
        Assert.Equal(45, officeTotals.Sum());

        // East Region holds no assets directly; it still appears with zeroed counters.
        var east = summary.EnumerateArray("offices")
            .Single(office => office.GetProperty("officeName").GetString() == "East Region");
        Assert.Equal(0, east.GetProperty("totalAssets").GetInt32());

        var laptops = summary.EnumerateArray("categories")
            .Single(category => category.GetProperty("categoryName").GetString() == "Laptops");
        Assert.Equal(12, laptops.GetProperty("totalAssets").GetInt32()); // 12 seeded laptops
    }

    [Fact]
    public async Task ExportRegisterExcel_ReturnsSpreadsheetMimeTypeAndZipContainer()
    {
        var response = await _client.GetAsync("/api/reports/register/excel", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        var payload = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.True(payload.Length > 0);
        Assert.Equal((byte)'P', payload[0]); // OOXML containers are ZIP archives
        Assert.Equal((byte)'K', payload[1]);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task ExportRegisterPdf_ReturnsPdfMimeTypeAndHeader()
    {
        var response = await _client.GetAsync("/api/reports/register/pdf", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var payload = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.True(payload.Length > 0);
        Assert.Equal((byte)'%', payload[0]);
        Assert.Equal((byte)'P', payload[1]);
        Assert.Equal((byte)'D', payload[2]);
        Assert.Equal((byte)'F', payload[3]);
    }
}
