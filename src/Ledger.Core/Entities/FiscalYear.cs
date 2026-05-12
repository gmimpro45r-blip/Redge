using Ledger.Core.Enums;
using Ledger.Shared;

namespace Ledger.Core.Entities;

public sealed class FiscalYear : Entity
{
    public Guid OrgId { get; private set; }
    public string Code { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalStatus Status { get; private set; } = FiscalStatus.Open;
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public Guid? RetainedEarningsAccountId { get; private set; }

    private FiscalYear() { }

    public static FiscalYear Create(Guid orgId, string code, DateOnly start, DateOnly end)
    {
        if (end <= start)
        {
            throw new ArgumentException("End date must be after start date.");
        }
        return new FiscalYear
        {
            OrgId = orgId,
            Code = Guard.NotNullOrWhiteSpace(code),
            StartDate = start,
            EndDate = end,
        };
    }

    public void Lock() => Status = FiscalStatus.Locked;

    public void Close(Guid userId, Guid retainedEarningsAccountId, DateTimeOffset at)
    {
        if (Status == FiscalStatus.Closed)
        {
            throw new InvalidOperationException("Fiscal year already closed.");
        }
        Status = FiscalStatus.Closed;
        ClosedAt = at;
        ClosedBy = userId;
        RetainedEarningsAccountId = retainedEarningsAccountId;
    }
}

public sealed class FiscalPeriod : Entity
{
    public Guid FiscalYearId { get; private set; }
    public string Code { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalStatus Status { get; private set; } = FiscalStatus.Open;

    private FiscalPeriod() { }

    public static FiscalPeriod Create(Guid yearId, string code, DateOnly start, DateOnly end) => new()
    {
        FiscalYearId = yearId,
        Code = Guard.NotNullOrWhiteSpace(code),
        StartDate = start,
        EndDate = end,
    };

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public void Lock() => Status = FiscalStatus.Locked;
    public void Close() => Status = FiscalStatus.Closed;
}
