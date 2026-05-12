using System.Text.Json.Serialization;

namespace Ledger.Services.Licensing;

/// <summary>
/// Plaintext portion of an activation key. Signed by the vendor's RSA private key
/// during issuance; verified by <see cref="LicenseValidator"/> on the customer machine
/// using the embedded public key.
/// </summary>
public sealed record LicensePayload
{
    [JsonPropertyName("device_id")]
    public required string DeviceId { get; init; }

    [JsonPropertyName("customer")]
    public required string CustomerName { get; init; }

    [JsonPropertyName("edition")]
    public required string Edition { get; init; }      // Trial | Standard | Pro | Enterprise

    [JsonPropertyName("issued_at")]
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary><c>null</c> means perpetual.</summary>
    [JsonPropertyName("expires_at")]
    public DateOnly? ExpiresAt { get; init; }

    /// <summary>Free-text vendor reference (order number, invoice id...).</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }
}
