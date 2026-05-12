using System.Globalization;
using Ledger.Core.Entities;
using Ledger.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Data.Repositories;

public sealed class JournalRepository : IJournalRepository
{
    private readonly LedgerDbContext _db;

    public JournalRepository(LedgerDbContext db) => _db = db;

    public Task<JournalHeader?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.JournalHeaders.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JournalHeader>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.JournalHeaders.OrderByDescending(h => h.EntryDate).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task AddAsync(JournalHeader entity, CancellationToken cancellationToken = default)
        => _db.JournalHeaders.AddAsync(entity, cancellationToken).AsTask();

    public void Update(JournalHeader entity) => _db.JournalHeaders.Update(entity);

    public Task<JournalHeader?> GetWithLinesAsync(Guid headerId, CancellationToken cancellationToken = default)
        => _db.JournalHeaders
            .Include(h => h.Lines)
            .FirstOrDefaultAsync(h => h.Id == headerId, cancellationToken);

    public async Task<string> NextEntryNoAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        // Format: "JE-YYYY-NNNN"  (per-org running number per calendar year).
        var year = DateTime.UtcNow.Year;
        var prefix = $"JE-{year}-";
        var last = await _db.JournalHeaders
            .Where(h => h.OrgId == orgId && h.EntryNo.StartsWith(prefix))
            .OrderByDescending(h => h.EntryNo)
            .Select(h => h.EntryNo)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            next = n + 1;
        }
        return $"{prefix}{next:D4}";
    }
}
