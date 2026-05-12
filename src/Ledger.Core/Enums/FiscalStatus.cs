namespace Ledger.Core.Enums;

/// <summary>
/// Lifecycle of a <see cref="Ledger.Core.Entities.FiscalYear"/> or <see cref="Ledger.Core.Entities.FiscalPeriod"/>.
/// </summary>
public enum FiscalStatus
{
    Open,
    Locked,
    Closed,
}
