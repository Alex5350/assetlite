using System.Net;
using Xunit;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Printable label endpoint: barcode and QR SVG artwork for a seeded tag.</summary>
public sealed class AssetLabelApiTests : IClassFixture<AssetLiteApiFactory>
{
    private readonly HttpClient _client;

    public AssetLabelApiTests(AssetLiteApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetLabel_WithSeededTag_ReturnsBarcodeAndQrSvg()
    {
        var response = await _client.GetAsync("/api/assets/AST-000001/label", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var label = (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ParseJson();

        Assert.Equal("AST-000001", label.GetString("tag"));
        Assert.Equal("AST-000001", label.GetString("labelText"));
        Assert.StartsWith("<svg", label.GetString("barcodeSvg").TrimStart());
        Assert.StartsWith("<svg", label.GetString("qrSvg").TrimStart());
        Assert.Contains("</svg>", label.GetString("barcodeSvg"));
        Assert.Contains("</svg>", label.GetString("qrSvg"));
    }

    [Fact]
    public async Task GetLabel_WithUnknownTag_Returns404()
    {
        var response = await _client.GetAsync("/api/assets/AST-999999/label", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLabel_WithMalformedTag_Returns400()
    {
        var response = await _client.GetAsync("/api/assets/notatag/label", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
