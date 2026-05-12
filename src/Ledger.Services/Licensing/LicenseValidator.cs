using System.Security.Cryptography;
using Ledger.Shared;

namespace Ledger.Services.Licensing;

/// <summary>
/// Validates an activation key against the vendor's RSA public key and the current device.
/// Construct one instance with the embedded public key (PEM string) and reuse it.
/// </summary>
public sealed class LicenseValidator
{
    private readonly RSA _publicKey;
    private readonly IHardwareIdentifier _hardware;

    public LicenseValidator(string publicKeyPem, IHardwareIdentifier hardware)
    {
        _publicKey = RSA.Create();
        _publicKey.ImportFromPem(publicKeyPem);
        _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
    }

    public Result<LicensePayload> Validate(string activationKey, DateOnly today)
    {
        byte[] payloadBytes;
        byte[] signature;
        try
        {
            (payloadBytes, signature) = LicenseKeyCodec.Decode(activationKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return Result<LicensePayload>.Fail($"INVALID_FORMAT: {ex.Message}");
        }

        bool ok;
        try
        {
            ok = _publicKey.VerifyData(
                payloadBytes,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException ex)
        {
            return Result<LicensePayload>.Fail($"INVALID_SIGNATURE: {ex.Message}");
        }

        if (!ok)
        {
            return Result<LicensePayload>.Fail("INVALID_SIGNATURE");
        }

        LicensePayload payload;
        try
        {
            payload = LicenseKeyCodec.DeserializePayload(payloadBytes);
        }
        catch (Exception ex)
        {
            return Result<LicensePayload>.Fail($"INVALID_PAYLOAD: {ex.Message}");
        }

        var actualDevice = _hardware.GetDeviceId();
        if (!string.Equals(payload.DeviceId, actualDevice, StringComparison.OrdinalIgnoreCase))
        {
            return Result<LicensePayload>.Fail("DEVICE_MISMATCH");
        }

        if (payload.ExpiresAt.HasValue && payload.ExpiresAt.Value <= today)
        {
            return Result<LicensePayload>.Fail("EXPIRED");
        }

        return Result<LicensePayload>.Ok(payload);
    }
}
