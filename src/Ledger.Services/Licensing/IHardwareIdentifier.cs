namespace Ledger.Services.Licensing;

/// <summary>
/// Returns a stable fingerprint of the current machine. The hash combines several
/// hardware-level identifiers (CPU, motherboard, BIOS) so the device ID is identical
/// across reboots but unique per physical PC.
/// </summary>
public interface IHardwareIdentifier
{
    /// <summary>SHA-256 hex string of the combined hardware identifiers.</summary>
    string GetDeviceId();
}
