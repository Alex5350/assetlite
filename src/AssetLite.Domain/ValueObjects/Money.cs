using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;

namespace AssetLite.Domain.ValueObjects;

/// <summary>
/// An immutable monetary amount with a 3-letter ISO 4217 currency code (default
/// <see cref="DefaultCurrency"/>). Amounts are normalized to two decimal places.
/// </summary>
public sealed record Money
{
    /// <summary>The currency used when none is specified.</summary>
    public const string DefaultCurrency = "USD";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Gets the normalized amount with two decimal places (never negative).</summary>
    public decimal Amount { get; }

    /// <summary>Gets the 3-letter uppercase ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a monetary value. The amount is rounded to two decimal places using banker's
    /// rounding (<see cref="MidpointRounding.ToEven"/>); <paramref name="currency"/> defaults to
    /// <see cref="DefaultCurrency"/> when null or whitespace and is normalized to uppercase.
    /// </summary>
    /// <param name="amount">The amount; must not be negative.</param>
    /// <param name="currency">Optional 3-letter currency code.</param>
    /// <returns>
    /// A successful result with the value, or <see cref="ValueObjectErrors.NegativeAmount"/> or
    /// <see cref="ValueObjectErrors.InvalidCurrency"/>.
    /// </returns>
    public static DomainResult<Money> Create(decimal amount, string? currency = null)
    {
        if (amount < 0)
        {
            return DomainResult<Money>.Failure(ValueObjectErrors.NegativeAmount);
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? DefaultCurrency
            : currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || !IsUpperAlphaAscii(normalizedCurrency))
        {
            return DomainResult<Money>.Failure(ValueObjectErrors.InvalidCurrency);
        }

        return DomainResult<Money>.Success(
            new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), normalizedCurrency));
    }

    private static bool IsUpperAlphaAscii(string value)
    {
        foreach (var character in value)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return true;
    }
}
