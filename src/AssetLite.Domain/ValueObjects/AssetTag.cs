using System.Globalization;

using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;

namespace AssetLite.Domain.ValueObjects;

/// <summary>
/// An immutable asset tag in the canonical format <c>AST-dddddd</c>, e.g. <c>AST-000123</c>.
/// Tags are zero-padded six-digit numbers between 000001 and 999999.
/// </summary>
/// <remarks>
/// Tags are allocated sequentially (AST-000001, AST-000002, ...) by the
/// <c>IAssetTagAllocator</c> port in the Application layer; this value object only models and
/// validates the format.
/// </remarks>
public sealed record AssetTag
{
    /// <summary>The canonical tag prefix.</summary>
    public const string Prefix = "AST-";

    /// <summary>The smallest valid tag number (AST-000001).</summary>
    public const int MinNumber = 1;

    /// <summary>The largest valid tag number (AST-999999).</summary>
    public const int MaxNumber = 999_999;

    private AssetTag(int number) => Number = number;

    /// <summary>Gets the numeric part of the tag (1 - 999999).</summary>
    public int Number { get; }

    /// <summary>Gets the canonical formatted value, e.g. <c>AST-000123</c>.</summary>
    public string Value => ToString();

    /// <summary>Returns the canonical formatted tag, e.g. <c>AST-000123</c>.</summary>
    /// <returns>The formatted tag.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Prefix}{Number:D6}");

    /// <summary>
    /// Creates a tag from a tag number, validating the supported range
    /// (<see cref="MinNumber"/> - <see cref="MaxNumber"/>).
    /// </summary>
    /// <param name="number">The tag number.</param>
    /// <returns>A successful result with the tag, or <see cref="ValueObjectErrors.InvalidAssetTag"/>.</returns>
    public static DomainResult<AssetTag> FromNumber(int number) =>
        number is >= MinNumber and <= MaxNumber
            ? DomainResult<AssetTag>.Success(new AssetTag(number))
            : DomainResult<AssetTag>.Failure(ValueObjectErrors.InvalidAssetTag);

    /// <summary>
    /// Attempts to parse an exact canonical tag string (<c>AST-</c> + 6 digits, uppercase,
    /// no surrounding whitespace).
    /// </summary>
    /// <param name="text">The candidate tag text.</param>
    /// <param name="tag">The parsed tag, or <see langword="null"/> when parsing fails.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid tag.</returns>
    public static bool TryParse(string? text, out AssetTag? tag)
    {
        tag = null;
        if (text is not { Length: 10 } || !text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var number = 0;
        for (var index = Prefix.Length; index < text.Length; index++)
        {
            var character = text[index];
            if (character is < '0' or > '9')
            {
                return false;
            }

            number = (number * 10) + (character - '0');
        }

        if (number is < MinNumber or > MaxNumber)
        {
            return false;
        }

        tag = new AssetTag(number);
        return true;
    }

    /// <summary>Determines whether <paramref name="text"/> is a valid canonical tag string.</summary>
    /// <param name="text">The candidate tag text.</param>
    /// <returns><see langword="true"/> when the text parses to a valid tag.</returns>
    public static bool IsValid(string? text) => TryParse(text, out _);
}
