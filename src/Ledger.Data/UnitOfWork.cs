using Ledger.Data.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ledger.Data;

/// <summary>
/// EF Core-backed unit of work. One context = one logical transaction scope.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LedgerDbContext _db;
    private IDbContextTransaction? _tx;

    public UnitOfWork(LedgerDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _tx = await _db.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_tx is null) return;
        await _tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        await _tx.DisposeAsync().ConfigureAwait(false);
        _tx = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_tx is null) return;
        await _tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await _tx.DisposeAsync().ConfigureAwait(false);
        _tx = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_tx is not null) await _tx.DisposeAsync().ConfigureAwait(false);
        await _db.DisposeAsync().ConfigureAwait(false);
    }
}
