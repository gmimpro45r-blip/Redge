using FluentAssertions;
using Ledger.Core.ValueObjects;

namespace Ledger.Core.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_rounds_to_four_decimal_places()
    {
        var m = new Money(1.123456789m, "usd");
        m.Amount.Should().Be(1.1235m);
        m.Currency.Should().Be("USD"); // normalised
    }

    [Fact]
    public void Addition_same_currency_succeeds()
    {
        var a = new Money(10.50m, "USD");
        var b = new Money(0.0005m, "USD");
        (a + b).Should().Be(new Money(10.5005m, "USD"));
    }

    [Fact]
    public void Addition_different_currency_throws()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");
        var act = () => _ = a + b;
        act.Should().Throw<InvalidOperationException>().WithMessage("Currency mismatch*");
    }

    [Theory]
    [InlineData(100, 1.10, 110.0000, "EUR")]
    [InlineData(50.5, 30.5, 1540.2500, "EGP")]
    public void Convert_uses_exchange_rate(decimal amount, decimal rate, decimal expected, string target)
    {
        var m = new Money(amount, "USD").ConvertTo(target, rate);
        m.Amount.Should().Be(expected);
        m.Currency.Should().Be(target);
    }

    [Fact]
    public void Convert_negative_rate_throws()
    {
        var act = () => new Money(1m, "USD").ConvertTo("EUR", -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
