using Ledger.Core.Entities;
using Ledger.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Data.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly LedgerDbContext _db;

    public AccountRepository(LedgerDbContext db) => _db = db;

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Accounts.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task AddAsync(Account entity, CancellationToken cancellationToken = default)
        => _db.Accounts.AddAsync(entity, cancellationToken).AsTask();

    public void Update(Account entity) => _db.Accounts.Update(entity);

    public Task<Account?> GetByCodeAsync(Guid orgId, string code, CancellationToken cancellationToken = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.OrgId == orgId && a.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByOrgAsync(Guid orgId, CancellationToken cancellationToken = default)
        => await _db.Accounts.Where(a => a.OrgId == orgId).OrderBy(a => a.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
}
