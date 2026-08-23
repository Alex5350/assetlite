using AssetLite.Domain.Errors;
using AssetLite.Domain.ValueObjects;
using Xunit;

namespace AssetLite.Domain.UnitTests.ValueObjects;

/// <summary>Unit tests for the <see cref="AssetTag"/> value object.</summary>
public sealed class AssetTagTests
{
    [Theory]
    [InlineData(1, "AST-000001")]
    [InlineData(42, "AST-000042")]
    [InlineData(123456, "AST-123456")]
    [InlineData(999999, "AST-999999")]
    public void FromNumber_WithNumberInsideBounds_ReturnsTagWithCanonicalValue(int number, string expected)
    {
        var result = AssetTag.FromNumber(number);

        Assert.True(result.IsSuccess);
        var tag = result.GetValueOrThrow();
        Assert.Equal(number, tag.Number);
        Assert.Equal(expected, tag.Value);
        Assert.Equal(expected, tag.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999999)]
    [InlineData(1_000_000)]
    [InlineData(int.MaxValue)]
    public void FromNumber_WithNumberOutsideBounds_ReturnsInvalidAssetTagError(int number)
    {
        var result = AssetTag.FromNumber(number);

        Assert.True(result.IsFailure);
        Assert.Equal(ValueObjectErrors.InvalidAssetTag, result.Error);
        Assert.Equal("AssetTag.Invalid", result.Error!.Code);
    }

    [Fact]
    public void FromNumber_AtBoundsEdges_AcceptsExactlyMinAndMax()
    {
        // Arrange / Act
        var min = AssetTag.FromNumber(AssetTag.MinNumber);
        var max = AssetTag.FromNumber(AssetTag.MaxNumber);

        // Assert
        Assert.True(min.IsSuccess);
        Assert.True(max.IsSuccess);
        Assert.Equal("AST-000001", min.GetValueOrThrow().Value);
        Assert.Equal("AST-999999", max.GetValueOrThrow().Value);
    }

    [Theory]
    [InlineData("AST-000001")]
    [InlineData("AST-000042")]
    [InlineData("AST-999999")]
    public void TryParse_WithCanonicalTag_ReturnsTrueAndParsesTag(string text)
    {
        var parsed = AssetTag.TryParse(text, out var tag);

        Assert.True(parsed);
        Assert.NotNull(tag);
        Assert.Equal(text, tag!.Value);
    }

    [Theory]
    [InlineData(" AST-000001")]   // leading whitespace
    [InlineData("AST-000001 ")]   // trailing whitespace
    [InlineData("ast-000001")]    // wrong prefix case
    [InlineData("ASX-000001")]    // wrong prefix
    [InlineData("AST:000001")]    // wrong separator
    [InlineData("AST-00001")]     // too short (5 digits)
    [InlineData("AST-0000010")]   // too long (7 digits)
    [InlineData("AST-0000AB")]    // letters in the number part
    [InlineData("AST-000000")]    // number below the minimum
    [InlineData("AST-1000000")]   // number above the maximum (7 digits)
    [InlineData("")]
    [InlineData("          ")]
    public void TryParse_WithMalformedText_ReturnsFalseAndNullTag(string? text)
    {
        var parsed = AssetTag.TryParse(text, out var tag);

        Assert.False(parsed);
        Assert.Null(tag);
    }

    [Fact]
    public void TryParse_WithNull_ReturnsFalseAndNullTag()
    {
        var parsed = AssetTag.TryParse(null, out var tag);

        Assert.False(parsed);
        Assert.Null(tag);
    }

    [Theory]
    [InlineData("AST-000123", true)]
    [InlineData("AST-000000", false)]
    [InlineData("AST-12345", false)]
    [InlineData("not-a-tag", false)]
    [InlineData("", false)]
    public void IsValid_MatchesTryParseOutcome(string? text, bool expected)
    {
        Assert.Equal(expected, AssetTag.IsValid(text));
        Assert.Equal(expected, AssetTag.TryParse(text, out _));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.False(AssetTag.IsValid(null));
    }

    [Fact]
    public void Equality_TagsWithSameNumber_AreEqual()
    {
        var fromNumber = AssetTag.FromNumber(7).GetValueOrThrow();
        AssetTag.TryParse("AST-000007", out var parsed);

        Assert.Equal(fromNumber, parsed);
        Assert.Equal(fromNumber.GetHashCode(), parsed!.GetHashCode());
    }

    [Fact]
    public void Equality_TagsWithDifferentNumbers_AreNotEqual()
    {
        var left = AssetTag.FromNumber(7).GetValueOrThrow();
        var right = AssetTag.FromNumber(8).GetValueOrThrow();

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void FromNumber_ResultRoundTripsThroughTryParse()
    {
        var tag = AssetTag.FromNumber(654321).GetValueOrThrow();

        Assert.True(AssetTag.TryParse(tag.Value, out var reparsed));
        Assert.Equal(tag, reparsed);
    }
}
