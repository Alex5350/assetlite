using AssetLite.Domain.Errors;
using AssetLite.Domain.ValueObjects;
using Xunit;

namespace AssetLite.Domain.UnitTests.ValueObjects;

/// <summary>Unit tests for the <see cref="Money"/> value object.</summary>
public sealed class MoneyTests
{
    [Fact]
    public void Create_WithWholeAmount_RoundsToTwoDecimalPlaces()
    {
        var result = Money.Create(123m);

        Assert.True(result.IsSuccess);
        Assert.Equal(123.00m, result.GetValueOrThrow().Amount);
    }

    [Theory]
    [InlineData(123.456, 123.46)]
    [InlineData(99.999, 100.00)]
    [InlineData(0.1, 0.1)]
    public void Create_WithMoreThanTwoDecimals_RoundsToTwoDecimalPlaces(decimal amount, decimal expected)
    {
        var result = Money.Create(amount);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.GetValueOrThrow().Amount);
    }

    [Theory]
    [InlineData(10.005, 10.00)] // tie rounds to even last digit (0)
    [InlineData(10.015, 10.02)] // tie rounds to even last digit (2)
    [InlineData(0.125, 0.12)]
    [InlineData(0.135, 0.14)]
    public void Create_WithExactHalfCent_UsesBankersRounding(decimal amount, decimal expected)
    {
        var result = Money.Create(amount);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.GetValueOrThrow().Amount);
    }

    [Fact]
    public void Create_WithZero_ReturnsZeroAmount()
    {
        var result = Money.Create(0m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.GetValueOrThrow().Amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1234.56)]
    public void Create_WithNegativeAmount_ReturnsNegativeAmountError(decimal amount)
    {
        var result = Money.Create(amount);

        Assert.True(result.IsFailure);
        Assert.Equal(ValueObjectErrors.NegativeAmount, result.Error);
        Assert.Equal("Money.NegativeAmount", result.Error!.Code);
    }

    [Fact]
    public void Create_WithoutCurrency_DefaultsToUsd()
    {
        var result = Money.Create(19.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.GetValueOrThrow().Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceCurrency_DefaultsToUsd(string? currency)
    {
        var result = Money.Create(5m, currency);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.GetValueOrThrow().Currency);
    }

    [Theory]
    [InlineData("USD", "USD")]
    [InlineData("usd", "USD")]
    [InlineData("Eur", "EUR")]
    [InlineData(" eur ", "EUR")]
    public void Create_WithThreeLetterCurrency_TrimsAndNormalizesToUppercase(string currency, string expected)
    {
        var result = Money.Create(5m, currency);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.GetValueOrThrow().Currency);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("EURO")]
    [InlineData("US1")]
    [InlineData("12")]
    [InlineData("U$")]
    [InlineData("US-D")]
    public void Create_WithInvalidCurrency_ReturnsInvalidCurrencyError(string currency)
    {
        var result = Money.Create(5m, currency);

        Assert.True(result.IsFailure);
        Assert.Equal(ValueObjectErrors.InvalidCurrency, result.Error);
        Assert.Equal("Money.InvalidCurrency", result.Error!.Code);
    }

    [Fact]
    public void Create_RoundsAmountBeforeCheckingTwoDecimalPrecision()
    {
        var result = Money.Create(1.999m, "gbp");

        Assert.True(result.IsSuccess);
        var money = result.GetValueOrThrow();
        Assert.Equal(2.00m, money.Amount);
        Assert.Equal("GBP", money.Currency);
    }

    [Fact]
    public void Equality_MoneyWithSameAmountAndCurrency_AreEqual()
    {
        var left = Money.Create(10m, "USD").GetValueOrThrow();
        var right = Money.Create(10.00m, "usd").GetValueOrThrow();

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_MoneyWithDifferentCurrency_AreNotEqual()
    {
        var left = Money.Create(10m, "USD").GetValueOrThrow();
        var right = Money.Create(10m, "EUR").GetValueOrThrow();

        Assert.NotEqual(left, right);
    }
}
