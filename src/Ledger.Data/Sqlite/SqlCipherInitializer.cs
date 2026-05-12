namespace Ledger.Data.Sqlite;

/// <summary>
/// Initialises the SQLitePCLRaw native bundle. Must be called once during app startup,
/// before any <see cref="LedgerDbContext"/> is constructed.
/// </summary>
public static class SqlCipherInitializer
{
    private static int _initialised;

    public static void EnsureInitialised()
    {
        if (Interlocked.CompareExchange(ref _initialised, 1, 0) == 0)
        {
            // SQLitePCL.Batteries_V2.Init() is provided by SQLitePCLRaw.bundle_e_sqlcipher
            // which is referenced by Ledger.Data.csproj. It loads the SQLCipher-aware
            // native SQLite library so that PRAGMA key works.
            SQLitePCL.Batteries_V2.Init();
        }
    }
}
