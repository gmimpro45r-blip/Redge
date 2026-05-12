using Ledger.Shared;

namespace Ledger.Core.Entities;

public sealed class Currency : Entity
{
    public string Code { get; private set; } = default!;        // ISO-4217
    public string Name { get; private set; } = default!;
    public string? Symbol { get; private set; }
    public short Decimals { get; private set; } = 2;
    public bool IsActive { get; private set; } = true;

    private Currency() { }

    public static Currency Create(string code, string name, string? symbol = null, short decimals = 2) => new()
    {
        Code = Guard.NotNullOrWhiteSpace(code).ToUpperInvariant(),
        Name = Guard.NotNullOrWhiteSpace(name),
        Symbol = symbol,
        Decimals = decimals,
    };
}
