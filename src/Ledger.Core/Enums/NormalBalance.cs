namespace Ledger.Core.Enums;

/// <summary>
/// Which side of the ledger increases this account's balance.
/// Assets &amp; Expenses are <see cref="Debit"/>; Liabilities, Equity &amp; Revenue are <see cref="Credit"/>.
/// </summary>
public enum NormalBalance
{
    Debit,
    Credit,
}
