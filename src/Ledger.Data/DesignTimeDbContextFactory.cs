using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ledger.Data;

/// <summary>
/// Used only by EF Core tools (<c>dotnet ef migrations add ...</c>) to construct a
/// <see cref="LedgerDbContext"/> without needing the SQLCipher native binary or a
/// real database key. The model that comes out matches the runtime context because
/// schema is defined entirely in <c>OnModelCreating</c>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new LedgerDbContext(options);
    }
}
