using BCryptNet = BCrypt.Net.BCrypt;

namespace Ledger.Services.Auth;

/// <summary>
/// Thin wrapper around BCrypt.Net-Next so callers don't take a hard dependency on it.
/// Work factor 12 is the 2025 OWASP minimum; tune up if running on fast hardware.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return BCryptNet.HashPassword(plainText, WorkFactor);
    }

    public bool Verify(string plainText, string hash)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(hash))
        {
            return false;
        }
        try
        {
            return BCryptNet.Verify(plainText, hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
