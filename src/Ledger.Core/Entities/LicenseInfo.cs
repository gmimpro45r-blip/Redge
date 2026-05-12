using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// Record of the activation key that unlocked this installation, plus its terms.
/// Only the latest active record is honoured; historical rows are retained for audit.
/// </summary>
public sealed class LicenseInfo : Entity
{
    /// <summary>Hardware fingerprint of the machine the key was bound to.</summary>
    public string DeviceId { get; private set; } = default!;

    /// <summary>The activation key as entered by the user (Base64 of signed payload).</summary>
    public string ActivationKey { get; private set; } = default!;

    /// <summary>Customer / licensee name from the payload.</summary>
    public string CustomerName { get; private set; } = default!;

    /// <summary>Edition: Trial / Standard / Pro / Enterprise.</summary>
    public string Edition { get; private set; } = "Standard";

    public DateTimeOffset ActivatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Expiry date (exclusive). <c>null</c> = perpetual license.</summary>
    public DateOnly? ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    private LicenseInfo() { }

    public static LicenseInfo Create(
        string deviceId,
        string activationKey,
        string customerName,
        string edition,
        DateOnly? expiresAt)
    {
        return new LicenseInfo
        {
            DeviceId = Guard.NotNullOrWhiteSpace(deviceId),
            ActivationKey = Guard.NotNullOrWhiteSpace(activationKey),
            CustomerName = Guard.NotNullOrWhiteSpace(customerName),
            Edition = Guard.NotNullOrWhiteSpace(edition),
            ExpiresAt = expiresAt,
        };
    }

    public void Revoke() => IsRevoked = true;

    public bool IsCurrentlyValid(DateOnly today)
    {
        if (IsRevoked)
        {
            return false;
        }
        return !ExpiresAt.HasValue || ExpiresAt.Value > today;
    }
}
