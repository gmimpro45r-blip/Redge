using Ledger.Core.Abstractions;
using Ledger.Core.Enums;
using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// Aggregate root for a journal entry. Enforces the cornerstone invariants:
/// <list type="bullet">
///   <item>Σdebit == Σcredit before posting.</item>
///   <item>Posted entries are immutable — corrections happen via <see cref="CreateReversal"/>.</item>
///   <item>At least one line, and every line has exactly one of debit/credit &gt; 0.</item>
/// </list>
/// </summary>
public sealed class JournalHeader : Entity
{
    public Guid OrgId { get; private set; }
    public string EntryNo { get; private set; } = default!;
    public DateOnly EntryDate { get; private set; }
    public string? Description { get; private set; }
    public string? Reference { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal ExchangeRate { get; private set; } = 1m;
    public Guid? FiscalPeriodId { get; private set; }
    public EntryStatus Status { get; private set; } = EntryStatus.Draft;
    public Guid? ReversedById { get; private set; }
    public Guid? ReversesId { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; private set; }

    private readonly List<JournalLine> _lines = new();
    public IReadOnlyList<JournalLine> Lines => _lines;

    private JournalHeader() { }

    public static JournalHeader CreateDraft(
        Guid orgId,
        string entryNo,
        DateOnly entryDate,
        string currency,
        decimal exchangeRate,
        Guid? createdBy = null,
        string? description = null,
        string? reference = null,
        Guid? fiscalPeriodId = null)
    {
        return new JournalHeader
        {
            OrgId = orgId,
            EntryNo = Guard.NotNullOrWhiteSpace(entryNo),
            EntryDate = entryDate,
            Currency = Guard.NotNullOrWhiteSpace(currency),
            ExchangeRate = Guard.Positive(exchangeRate),
            Description = description,
            Reference = reference,
            FiscalPeriodId = fiscalPeriodId,
            CreatedBy = createdBy,
        };
    }

    public JournalLine AddLine(
        Guid accountId,
        decimal debit,
        decimal credit,
        string? description = null,
        Guid? costCenterId = null)
    {
        EnsureEditable();
        var line = JournalLine.Create(
            lineNo: (short)(_lines.Count + 1),
            accountId: accountId,
            debit: debit,
            credit: credit,
            description: description,
            costCenterId: costCenterId);
        line.Attach(Id, ExchangeRate);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(short lineNo)
    {
        EnsureEditable();
        var line = _lines.FirstOrDefault(l => l.LineNo == lineNo)
            ?? throw new InvalidOperationException($"Line #{lineNo} not found.");
        _lines.Remove(line);
        Renumber();
    }

    public decimal TotalDebit => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);
    public bool IsBalanced
        => decimal.Round(TotalDebit, 4) == decimal.Round(TotalCredit, 4)
           && TotalDebit > 0m;

    /// <summary>Post the entry, making it immutable and visible in reports.</summary>
    public Result Post(IClock clock, Guid userId)
    {
        if (Status != EntryStatus.Draft)
        {
            return Result.Fail($"INVALID_STATUS: cannot post a {Status} entry.");
        }
        if (_lines.Count == 0)
        {
            return Result.Fail("EMPTY_ENTRY: at least one line required.");
        }
        if (!IsBalanced)
        {
            return Result.Fail($"UNBALANCED: debit={TotalDebit} credit={TotalCredit}.");
        }

        Status = EntryStatus.Posted;
        PostedAt = clock.UtcNow;
        PostedBy = userId;
        return Result.Ok();
    }

    /// <summary>
    /// Build the inverse of this entry. Caller is responsible for persisting both and
    /// linking them via <see cref="ReversesId"/> / <see cref="ReversedById"/>.
    /// </summary>
    public JournalHeader CreateReversal(IClock clock, Guid userId, string reversalEntryNo, string? reason = null)
    {
        if (Status != EntryStatus.Posted)
        {
            throw new InvalidOperationException("Only posted entries can be reversed.");
        }

        var reversal = new JournalHeader
        {
            OrgId = OrgId,
            EntryNo = Guard.NotNullOrWhiteSpace(reversalEntryNo),
            EntryDate = clock.Today,
            Currency = Currency,
            ExchangeRate = ExchangeRate,
            FiscalPeriodId = FiscalPeriodId,
            Status = EntryStatus.Reversal,
            ReversesId = Id,
            CreatedBy = userId,
            Description = reason ?? $"Reversal of {EntryNo}",
            PostedAt = clock.UtcNow,
            PostedBy = userId,
        };

        short n = 1;
        foreach (var l in _lines)
        {
            // swap debit/credit
            var inv = JournalLine.Create(n++, l.AccountId, l.Credit, l.Debit, l.Description, l.CostCenterId);
            inv.Attach(reversal.Id, ExchangeRate);
            reversal._lines.Add(inv);
        }

        // mark this entry as reversed
        Status = EntryStatus.Reversed;
        ReversedById = reversal.Id;
        return reversal;
    }

    private void EnsureEditable()
    {
        if (Status != EntryStatus.Draft)
        {
            throw new InvalidOperationException($"Entry is {Status} and cannot be edited.");
        }
    }

    private void Renumber()
    {
        for (short i = 0; i < _lines.Count; i++)
        {
            typeof(JournalLine)
                .GetProperty(nameof(JournalLine.LineNo))!
                .SetValue(_lines[i], (short)(i + 1));
        }
    }
}
