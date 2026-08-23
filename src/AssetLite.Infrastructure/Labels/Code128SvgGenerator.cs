using System.Globalization;
using System.Text;

namespace AssetLite.Infrastructure.Labels;

/// <summary>
/// Hand-rolled Code 128 (subset B/C) encoder producing a self-contained SVG with quiet zones and
/// human-readable text — deterministic, dependency-free, and safe to run anywhere (no imaging
/// package involved).
/// </summary>
/// <remarks>
/// <para>
/// Encoding follows ISO/IEC 15417: symbols are 11 modules wide, the stop pattern is 13 modules,
/// and the checksum is <c>(start + Σ value_i × i) mod 103</c> with i counting from 1 for the first
/// symbol after the start code.
/// </para>
/// <para>
/// Code-set selection: an all-digit input of even length is encoded entirely in code C; otherwise
/// encoding starts in code B and switches to code C for maximal runs of at least four consecutive
/// digits of even length (switching back with CODE B afterwards). For canonical asset tags
/// (<c>AST-dddddd</c>) this yields Start B, "AST-", CODE C, three digit pairs.
/// </para>
/// </remarks>
public sealed class Code128SvgGenerator
{
    /// <summary>Start code B symbol value.</summary>
    public const int StartCodeB = 104;

    /// <summary>Start code C symbol value.</summary>
    public const int StartCodeC = 105;

    /// <summary>CODE C switch symbol value (valid inside code B).</summary>
    public const int SwitchToCodeC = 99;

    /// <summary>CODE B switch symbol value (valid inside code C).</summary>
    public const int SwitchToCodeB = 100;

    /// <summary>Stop symbol value.</summary>
    public const int Stop = 106;

    /// <summary>Minimum quiet zone width in modules on each side (spec minimum).</summary>
    public const int QuietZoneModules = 10;

    private const int StopPatternModules = 13;
    private const int SymbolModules = 11;
    private const double ModuleWidth = 2.0; // px per module
    private const double BarHeight = 90.0; // px
    private const double TextHeight = 16.0; // px
    private const double TextGap = 6.0; // px between bars and human-readable text

    // Bar/space width tables for symbol values 0..106 (ISO/IEC 15417). Entries are six widths
    // (bar, space, bar, space, bar, space) summing to 11 modules; the stop pattern entry
    // (value 106) has seven widths summing to 13 modules.
    private static readonly string[] Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
    ];

    /// <summary>Encodes <paramref name="text"/> and returns the SVG markup for the barcode.</summary>
    /// <param name="text">Input text (printable ASCII, code B range 32..126; digits may use code C).</param>
    /// <returns>A complete SVG document string.</returns>
    /// <exception cref="ArgumentException">The text is null/empty or contains unsupported characters.</exception>
    public string Generate(string text)
    {
        var encoding = Encode(text);

        var totalModules = encoding.TotalModules;
        var width = (totalModules + (2 * QuietZoneModules)) * ModuleWidth;
        var height = BarHeight + TextGap + TextHeight;

        var bars = new StringBuilder();
        var x = QuietZoneModules * ModuleWidth;
        for (var index = 0; index < encoding.BarWidths.Count;)
        {
            var barWidth = encoding.BarWidths[index];
            var spaceWidth = index + 1 < encoding.BarWidths.Count ? encoding.BarWidths[index + 1] : 0;
            bars.Append(CultureInfo.InvariantCulture, $"""<rect x="{x:0.##}" y="0" width="{barWidth * ModuleWidth:0.##}" height="{BarHeight:0.##}"/>""");
            x += (barWidth + spaceWidth) * ModuleWidth;
            index += 2;
        }

        return $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="{width:0.##}" height="{height:0.##}" viewBox="0 0 {width:0.##} {height:0.##}" role="img" aria-label="Code 128 barcode {Escape(text)}">
                  <rect width="100%" height="100%" fill="#ffffff"/>
                  <g fill="#000000">
                {Indent(bars.ToString(), 4)}
                  </g>
                  <text x="{width / 2:0.##}" y="{BarHeight + TextGap + (TextHeight * 0.75):0.##}" font-family="Menlo, Consolas, monospace" font-size="{TextHeight:0.##}" text-anchor="middle" fill="#000000">{Escape(text)}</text>
                </svg>
                """;
    }

    /// <summary>
    /// Pure encoding step exposed for verification: returns the emitted symbol values (start,
    /// payload and code-set switches — checksum and stop are reported separately), the mod-103
    /// checksum symbol value and the total module count of the drawn barcode (quiet zones excluded).
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>The encoding breakdown (symbols, checksum, modules).</returns>
    /// <exception cref="ArgumentException">The text is null/empty or contains unsupported characters.</exception>
    public Code128Encoding Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Code 128 input must not be null or empty.", nameof(text));
        }

        var symbols = SelectSymbols(text);
        var checksum = symbols[0];
        for (var index = 1; index < symbols.Count; index++)
        {
            checksum += symbols[index] * index;
        }

        checksum %= 103;

        var barWidths = new List<int>(symbols.Count * 3 + 4);
        foreach (var symbol in symbols)
        {
            AppendPattern(barWidths, symbol);
        }

        AppendPattern(barWidths, checksum);
        AppendPattern(barWidths, Stop);

        // Data symbols (start + payload + switches) plus the checksum symbol, plus stop.
        var dataModules = ((symbols.Count + 1) * SymbolModules) + StopPatternModules;

        return new Code128Encoding([.. symbols], checksum, dataModules, barWidths);
    }

    private static List<int> SelectSymbols(string text)
    {
        foreach (var character in text)
        {
            if (character is < ' ' or > '~')
            {
                throw new ArgumentException(
                    $"Code 128 input supports printable ASCII only; found U+{((int)character):X4}.",
                    nameof(text));
            }
        }

        var isAllDigits = text.All(char.IsDigit);

        // Entirely digits of even length: pure code C.
        if (isAllDigits && text.Length % 2 == 0)
        {
            var pure = new List<int>(1 + (text.Length / 2)) { StartCodeC };
            for (var index = 0; index < text.Length; index += 2)
            {
                pure.Add((text[index] - '0') * 10 + (text[index + 1] - '0'));
            }

            return pure;
        }

        // Mixed content: start in code B and hop into code C for even digit runs of >= 4.
        var symbols = new List<int>(text.Length + 2) { StartCodeB };
        var inCodeC = false;
        var position = 0;
        while (position < text.Length)
        {
            var runLength = CountDigitRun(text, position);

            if (!inCodeC && runLength >= 4 && runLength % 2 == 0)
            {
                symbols.Add(SwitchToCodeC);
                inCodeC = true;
            }

            if (inCodeC)
            {
                var pairs = runLength / 2;
                for (var pair = 0; pair < pairs; pair++)
                {
                    symbols.Add((text[position] - '0') * 10 + (text[position + 1] - '0'));
                    position += 2;
                }

                if (position < text.Length)
                {
                    symbols.Add(SwitchToCodeB);
                    inCodeC = false;
                }
            }
            else
            {
                symbols.Add(text[position] - ' ');
                position++;
            }
        }

        return symbols;
    }

    private static int CountDigitRun(string text, int start)
    {
        var length = 0;
        while (start + length < text.Length && char.IsDigit(text[start + length]))
        {
            length++;
        }

        return length;
    }

    private static void AppendPattern(List<int> widths, int symbol)
    {
        foreach (var character in Patterns[symbol])
        {
            widths.Add(character - '0');
        }
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string Indent(string svg, int levels)
    {
        var prefix = new string(' ', levels * 2);
        using var reader = new StringReader(svg);
        var builder = new StringBuilder();
        while (reader.ReadLine() is { } line)
        {
            builder.Append(prefix).Append(line).Append('\n');
        }

        return builder.ToString().TrimEnd('\n');
    }
}

/// <summary>Breakdown of a Code 128 encoding, exposed for verification and testing.</summary>
/// <param name="Symbols">Emitted symbol values: start, payload and code-set switches (no checksum, no stop).</param>
/// <param name="Checksum">The mod-103 checksum symbol value.</param>
/// <param name="TotalModules">Total module count of the barcode: data symbols + checksum symbol + stop (quiet zones excluded).</param>
/// <param name="BarWidths">Alternating bar/space widths in modules, including checksum and stop.</param>
public sealed record Code128Encoding(
    IReadOnlyList<int> Symbols,
    int Checksum,
    int TotalModules,
    IReadOnlyList<int> BarWidths);
