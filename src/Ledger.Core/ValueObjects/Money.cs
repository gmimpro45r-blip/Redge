using Ledger.Shared;

namespace Ledger.Core.ValueObjects;

/// <summary>
/// Immutable money value object. All arithmetic uses <see cref="decimal"/> rounded to 4 dp
/// (matches the database column type <c>numeric(18,4)</c>).
/// Operations on differing currencies throw — convert explicitly via an exchange rate.
/// </summary>
public readonly record struct Money
{
    public const int Scale = 4;

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Currency = Guard.NotNullOrWhiteSpace(currency).ToUpperInvariant();
        Amount = decimal.Round(amount, Scale, MidpointRounding.ToEven);
    }

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public Money ConvertTo(string targetCurrency, decimal exchangeRate)
    {
        Guard.Positive(exchangeRate);
        return new Money(Amount * exchangeRate, targetCurrency);
    }

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static Money operator *(Money a, decimal f) => a.Multiply(f);

    public override string ToString() => $"{Amount.ToString("F" + Scale)} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Currency mismatch: {Currency} vs {other.Currency}. Convert explicitly via an exchange rate.");
        }
    }
}
