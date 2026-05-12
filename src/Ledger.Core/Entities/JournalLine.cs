using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// One debit-or-credit line of a journal entry. Exactly one of <see cref="Debit"/> or
/// <see cref="Credit"/> must be &gt; 0 — never both, never neither.
/// </summary>
public sealed class JournalLine : Entity
{
    public Guid HeaderId { get; private set; }
    public short LineNo { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string? Description { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public decimal DebitBase { get; private set; }
    public decimal CreditBase { get; private set; }

    private JournalLine() { }

    internal static JournalLine Create(
        short lineNo,
        Guid accountId,
        decimal debit,
        decimal credit,
        string? description = null,
        Guid? costCenterId = null)
    {
        Guard.NotNegative(debit);
        Guard.NotNegative(credit);
        if (debit == 0m && credit == 0m)
        {
            throw new ArgumentException("A line must have a non-zero debit or credit.");
        }
        if (debit > 0m && credit > 0m)
        {
            throw new ArgumentException("A line cannot be both debit and credit.");
        }

        return new JournalLine
        {
            LineNo = lineNo,
            AccountId = accountId,
            Debit = decimal.Round(debit, 4, MidpointRounding.ToEven),
            Credit = decimal.Round(credit, 4, MidpointRounding.ToEven),
            Description = description,
            CostCenterId = costCenterId,
        };
    }

    internal void Attach(Guid headerId, decimal exchangeRate)
    {
        HeaderId = headerId;
        DebitBase = decimal.Round(Debit * exchangeRate, 4, MidpointRounding.ToEven);
        CreditBase = decimal.Round(Credit * exchangeRate, 4, MidpointRounding.ToEven);
    }
}
