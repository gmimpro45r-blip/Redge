using Ledger.Core.Entities;
using Ledger.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ledger.Data;

/// <summary>
/// EF Core context backed by an SQLCipher-encrypted SQLite file. All financial data
/// lives in this single file (e.g. <c>%LOCALAPPDATA%\Redge\ledger.db</c>).
/// </summary>
public sealed class LedgerDbContext : DbContext
{
    private readonly SqlCipherConnectionFactory _connections;

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalHeader> JournalHeaders => Set<JournalHeader>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LicenseInfo> Licenses => Set<LicenseInfo>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public LedgerDbContext(SqlCipherConnectionFactory connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
    }

    /// <summary>Test-only constructor that accepts pre-configured options (e.g. InMemory provider).</summary>
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
    {
        _connections = null!;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            // Test scenarios — caller already wired UseInMemoryDatabase / UseSqlite / etc.
            return;
        }

        // Open an SQLCipher connection per context (EF Core will own its disposal).
        var conn = _connections.OpenConnection();
        optionsBuilder.UseSqlite(conn);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
    }

    /// <summary>
    /// Starts a transactional scope. <see cref="UnitOfWork"/> wraps this.
    /// </summary>
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);
}
