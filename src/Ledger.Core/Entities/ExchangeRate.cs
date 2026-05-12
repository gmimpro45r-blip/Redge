using Ledger.Shared;

namespace Ledger.Core.Entities;

public sealed class ExchangeRate : Entity
{
    public Guid OrgId { get; private set; }
    public string FromCurrency { get; private set; } = default!;
    public string ToCurrency { get; private set; } = default!;
    public decimal Rate { get; private set; }
    public DateOnly RateDate { get; private set; }
    public string Source { get; private set; } = "manual";

    private ExchangeRate() { }

    public static ExchangeRate Create(Guid orgId, string from, string to, decimal rate, DateOnly date, string source = "manual") => new()
    {
        OrgId = orgId,
        FromCurrency = Guard.NotNullOrWhiteSpace(from).ToUpperInvariant(),
        ToCurrency = Guard.NotNullOrWhiteSpace(to).ToUpperInvariant(),
        Rate = Guard.Positive(rate),
        RateDate = date,
        Source = source,
    };
}
