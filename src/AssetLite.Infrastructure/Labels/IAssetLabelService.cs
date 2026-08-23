namespace AssetLite.Infrastructure.Labels;

/// <summary>
/// Renders printable asset labels for a canonical asset tag: a hand-rolled Code 128 SVG barcode
/// and a QR code SVG pointing at the asset's public page.
/// </summary>
/// <remarks>
/// Defined in Infrastructure (not Application) because it is a presentation-adjacent capability
/// consumed by the API layer and the frontend, not by domain handlers.
/// </remarks>
public interface IAssetLabelService
{
    /// <summary>Generates the label artwork for a canonical asset tag (e.g. <c>AST-000001</c>).</summary>
    /// <param name="tag">The canonical asset tag string.</param>
    /// <returns>The barcode SVG, QR SVG and human-readable label text.</returns>
    /// <exception cref="ArgumentException">The tag is not a canonical asset tag.</exception>
    AssetLabel Generate(string tag);
}

/// <summary>The rendered label artifacts for one asset tag.</summary>
/// <param name="BarcodeSvg">Self-contained SVG of the Code 128 barcode with human-readable text.</param>
/// <param name="QrSvg">Self-contained SVG of the QR code linking to the asset's public page.</param>
/// <param name="LabelText">The human-readable label text (the canonical tag itself).</param>
public sealed record AssetLabel(
    string BarcodeSvg,
    string QrSvg,
    string LabelText);
