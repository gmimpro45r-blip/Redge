using System.Runtime.CompilerServices;

namespace Ledger.Shared;

/// <summary>
/// Tiny argument-guard helpers. Prefer these over hand-rolled if/throw blocks.
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class
        => value ?? throw new ArgumentNullException(name);

    public static string NotNullOrWhiteSpace(string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be null or whitespace.", name);
        }
        return value;
    }

    public static decimal NotNegative(decimal value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(name, value, "Value must be >= 0.");
        }
        return value;
    }

    public static decimal Positive(decimal value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(name, value, "Value must be > 0.");
        }
        return value;
    }
}
