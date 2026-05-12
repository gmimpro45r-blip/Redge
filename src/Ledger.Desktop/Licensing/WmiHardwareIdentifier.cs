using System.Management;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Ledger.Services.Licensing;

namespace Ledger.Desktop.Licensing;

/// <summary>
/// Windows-only implementation of <see cref="IHardwareIdentifier"/>. Combines
/// the CPU's <c>ProcessorId</c>, the motherboard serial, and the BIOS serial via WMI,
/// then hashes with SHA-256 to produce a stable per-machine fingerprint.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WmiHardwareIdentifier : IHardwareIdentifier
{
    public string GetDeviceId()
    {
        var cpu = WmiSingle("Win32_Processor", "ProcessorId");
        var board = WmiSingle("Win32_BaseBoard", "SerialNumber");
        var bios = WmiSingle("Win32_BIOS", "SerialNumber");
        var product = WmiSingle("Win32_ComputerSystemProduct", "UUID");

        var combined = string.Join("|", cpu, board, bios, product);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string WmiSingle(string @class, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {@class}");
            foreach (var mo in searcher.Get())
            {
                var value = mo[property]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }
        catch (ManagementException)
        {
            // Some WMI classes are blocked on locked-down systems. Fall through to the empty marker.
        }
        return "unknown";
    }
}
