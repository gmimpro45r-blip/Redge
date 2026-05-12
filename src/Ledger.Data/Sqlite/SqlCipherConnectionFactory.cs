using Microsoft.Data.Sqlite;

namespace Ledger.Data.Sqlite;

/// <summary>
/// Opens encrypted SQLite connections using SQLCipher. Every connection executes
/// <c>PRAGMA key = '...'</c> immediately after opening so subsequent queries
/// transparently encrypt/decrypt data on disk.
/// </summary>
public sealed class SqlCipherConnectionFactory
{
    private readonly string _databasePath;
    private readonly string _password;

    public SqlCipherConnectionFactory(string databasePath, string password)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path must be provided.", nameof(databasePath));
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password must be provided.", nameof(password));
        }
        _databasePath = databasePath;
        _password = password;

        SqlCipherInitializer.EnsureInitialised();
    }

    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        Pooling = false,
    }.ToString();

    /// <summary>
    /// Returns an *opened* SqliteConnection with SQLCipher key already applied.
    /// Caller owns disposal.
    /// </summary>
    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        ApplyKey(conn);
        ApplyPragmas(conn);
        return conn;
    }

    private void ApplyKey(SqliteConnection conn)
    {
        // Escape single quotes in the password per SQL string-literal rules.
        var escaped = _password.Replace("'", "''", StringComparison.Ordinal);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA key = '{escaped}';";
        cmd.ExecuteNonQuery();
    }

    private static void ApplyPragmas(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        // SQLCipher v4 defaults; explicit for forward-compat clarity.
        cmd.CommandText = """
            PRAGMA cipher_compatibility = 4;
            PRAGMA journal_mode  = WAL;
            PRAGMA synchronous   = NORMAL;
            PRAGMA foreign_keys  = ON;
            PRAGMA temp_store    = MEMORY;
            """;
        cmd.ExecuteNonQuery();
    }
}
