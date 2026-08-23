using AssetLite.Domain.Common;

namespace AssetLite.Domain.Errors;

/// <summary>
/// Stable error catalog for value-object shape violations. Codes use the value object name as
/// the prefix (e.g. <c>"AssetTag.Invalid"</c>).
/// </summary>
public static class ValueObjectErrors
{
    /// <summary>Tag number or tag string outside the supported format/range ("AssetTag.Invalid").</summary>
    public static readonly DomainError InvalidAssetTag = new(
        "AssetTag.Invalid",
        "Asset tag must be 'AST-' followed by 6 digits between 000001 and 999999.");

    /// <summary>Negative monetary amount ("Money.NegativeAmount").</summary>
    public static readonly DomainError NegativeAmount = new(
        "Money.NegativeAmount",
        "Monetary amounts cannot be negative.");

    /// <summary>Currency is not a 3-letter ISO 4217 code ("Money.InvalidCurrency").</summary>
    public static readonly DomainError InvalidCurrency = new(
        "Money.InvalidCurrency",
        "Currency must be a 3-letter ISO 4217 code (e.g. USD).");
}
