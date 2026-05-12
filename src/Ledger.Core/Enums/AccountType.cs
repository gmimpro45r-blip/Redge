namespace Ledger.Core.Enums;

/// <summary>
/// Top-level classification of an account in the Chart of Accounts.
/// Mirrors the <c>accounting.account_type</c> enum in PostgreSQL.
/// </summary>
public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
}
