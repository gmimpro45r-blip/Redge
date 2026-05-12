using System.Security.Cryptography;
using System.Text;

namespace Ledger.Services.Integration;

/// <summary>
/// Computes HMAC-SHA256 signatures for outgoing n8n webhook requests.
/// The signed payload is <c>{timestamp}.{json-body}</c>; the receiving
/// n8n workflow must validate both the signature and the timestamp freshness
/// (we use a 5-minute replay window).
/// </summary>
public static class N8nWebhookSigner
{
    public const string TimestampHeader = "X-Ledger-Timestamp";
    public const string SignatureHeader = "X-Ledger-Signature";
    public static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(5);

    public static string Sign(string body, long unixTimestampSeconds, string secret)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var data = $"{unixTimestampSeconds}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(
        string body,
        long unixTimestampSeconds,
        string providedSignature,
        string secret,
        DateTimeOffset now)
    {
        var ageSeconds = Math.Abs(now.ToUnixTimeSeconds() - unixTimestampSeconds);
        if (ageSeconds > ReplayWindow.TotalSeconds)
        {
            return false;
        }

        var expected = Sign(body, unixTimestampSeconds, secret);
        // constant-time comparison
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(providedSignature ?? string.Empty));
    }
}
