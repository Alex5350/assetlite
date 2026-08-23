using Microsoft.Extensions.Configuration;

namespace AssetLite.Infrastructure.Labels;

/// <summary>
/// Default <see cref="IAssetLabelService"/>: Code 128 SVG from the in-house encoder plus a QR code
/// SVG from QRCoder encoding <c>{PublicBaseUrl}/assets/{tag}</c>.
/// </summary>
internal sealed class AssetLabelService(IConfiguration configuration) : IAssetLabelService
{
    private const string PublicBaseUrlKey = "AssetLabels:PublicBaseUrl";
    private const string DefaultPublicBaseUrl = "http://localhost:5070";

    private static readonly Code128SvgGenerator Code128 = new();

    /// <inheritdoc />
    public AssetLabel Generate(string tag)
    {
        if (!Domain.ValueObjects.AssetTag.TryParse(tag, out var parsed) || parsed is null)
        {
            throw new ArgumentException($"'{tag}' is not a canonical asset tag (expected AST-dddddd).", nameof(tag));
        }

        var baseUrl = configuration[PublicBaseUrlKey];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultPublicBaseUrl;
        }

        var payload = $"{baseUrl.TrimEnd('/')}/assets/{parsed.Value}";

        using var generator = new QRCoder.QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCoder.SvgQRCode(data);
        var qrSvg = qr.GetGraphic(pixelsPerModule: 4, "#000000", "#ffffff", drawQuietZones: true);

        return new AssetLabel(Code128.Generate(parsed.Value), qrSvg, parsed.Value);
    }
}
