using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Ledger.Services.Licensing;

namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// Derives the SQLCipher database key from a machine-bound secret. The secret is
/// generated once and then encrypted with Windows DPAPI (CurrentUser scope), so the
/// key only round-trips through the original user profile on the original machine.
/// Even an offline attacker who copies <c>ledger.db</c> + <c>db.key</c> to another
/// PC cannot decrypt the database.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DatabaseKeyProvider
{
    private readonly string _keyFilePath;
    private readonly IHardwareIdentifier _hardware;

    public DatabaseKeyProvider(string appDataDirectory, IHardwareIdentifier hardware)
    {
        Directory.CreateDirectory(appDataDirectory);
        _keyFilePath = Path.Combine(appDataDirectory, "db.key");
        _hardware = hardware;
    }

    public string GetOrCreateKey()
    {
        byte[] secret;
        if (File.Exists(_keyFilePath))
        {
            var encrypted = File.ReadAllBytes(_keyFilePath);
            secret = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
        }
        else
        {
            secret = RandomNumberGenerator.GetBytes(32);
            var encrypted = ProtectedData.Protect(secret, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_keyFilePath, encrypted);
        }

        // Mix in the hardware id so a stolen db.key file still cannot decrypt the
        // database without the original machine.
        var deviceId = _hardware.GetDeviceId();
        var seed = new byte[secret.Length + Encoding.UTF8.GetByteCount(deviceId)];
        Buffer.BlockCopy(secret, 0, seed, 0, secret.Length);
        Encoding.UTF8.GetBytes(deviceId, seed.AsSpan(secret.Length));

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(seed, hash);
        return Convert.ToHexString(hash);
    }
}
