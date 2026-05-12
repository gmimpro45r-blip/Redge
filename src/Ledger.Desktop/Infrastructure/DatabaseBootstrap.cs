using Ledger.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// Opens the SQLCipher-encrypted SQLite database and applies any pending migrations
/// at startup. Runs exactly once when the app launches.
/// </summary>
public sealed class DatabaseBootstrap
{
    private readonly LedgerDbContext _db;

    public DatabaseBootstrap(LedgerDbContext db) => _db = db;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
