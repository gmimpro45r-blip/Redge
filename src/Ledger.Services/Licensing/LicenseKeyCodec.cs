using System.Text.Json;

namespace Ledger.Services.Licensing;

/// <summary>
/// Encodes / decodes an activation key wire format:
/// <c>{base64url-payload}.{base64url-signature}</c>
/// </summary>
internal static class LicenseKeyCodec
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static (byte[] PayloadBytes, byte[] Signature) Decode(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Activation key must be provided.", nameof(key));
        }

        var parts = key.Trim().Split('.');
        if (parts.Length != 2)
        {
            throw new FormatException("Activation key must contain exactly one '.' separator.");
        }

        var payload = Base64UrlDecode(parts[0]);
        var sig = Base64UrlDecode(parts[1]);
        return (payload, sig);
    }

    public static string Encode(byte[] payloadBytes, byte[] signature)
        => $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";

    public static byte[] SerializePayload(LicensePayload payload)
        => JsonSerializer.SerializeToUtf8Bytes(payload, Json);

    public static LicensePayload DeserializePayload(byte[] bytes)
        => JsonSerializer.Deserialize<LicensePayload>(bytes, Json)
           ?? throw new InvalidOperationException("Empty license payload.");

    private static string Base64UrlEncode(byte[] data)
    {
        var s = Convert.ToBase64String(data);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
