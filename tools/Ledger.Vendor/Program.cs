using Ledger.Services.Licensing;

namespace Ledger.Vendor;

/// <summary>
/// Vendor-side CLI for issuing Ledger activation keys.
///
/// Usage:
///   <c>Ledger.Vendor issue --private &lt;path&gt; --device &lt;id&gt; --customer &lt;name&gt;
///        [--edition Standard] [--expires 2027-01-01] [--reference INV-001]</c>
///
///   <c>Ledger.Vendor keygen --out &lt;dir&gt;</c>
///       Generates a fresh RSA-2048 keypair. Use this <b>once</b> to bootstrap your
///       commercial signing key. Keep the private PEM offline.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return (args.FirstOrDefault() ?? "").ToLowerInvariant() switch
            {
                "issue" => Issue(args[1..]),
                "keygen" => KeyGen(args[1..]),
                _ => Help(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private static int Issue(string[] args)
    {
        var opts = ParseArgs(args);
        var privatePath = opts.GetValueOrDefault("private")
            ?? throw new ArgumentException("--private <path-to-vendor_private.pem> is required");
        var deviceId = opts.GetValueOrDefault("device")
            ?? throw new ArgumentException("--device <device-id> is required");
        var customer = opts.GetValueOrDefault("customer")
            ?? throw new ArgumentException("--customer <name> is required");
        var edition = opts.GetValueOrDefault("edition") ?? "Standard";
        var reference = opts.GetValueOrDefault("reference");
        var expiresRaw = opts.GetValueOrDefault("expires");

        var pem = File.ReadAllText(privatePath);
        var gen = new LicenseKeyGenerator(pem);
        var payload = new LicensePayload
        {
            DeviceId = deviceId,
            CustomerName = customer,
            Edition = edition,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = string.IsNullOrWhiteSpace(expiresRaw)
                ? null
                : DateOnly.Parse(expiresRaw),
            Reference = reference,
        };

        var key = gen.Issue(payload);
        Console.WriteLine(key);
        return 0;
    }

    private static int KeyGen(string[] args)
    {
        var opts = ParseArgs(args);
        var outDir = opts.GetValueOrDefault("out") ?? ".";
        Directory.CreateDirectory(outDir);
        var (priv, pub) = LicenseKeyGenerator.CreateKeyPair(2048);
        var privPath = Path.Combine(outDir, "vendor_private.pem");
        var pubPath = Path.Combine(outDir, "vendor_public.pem");
        File.WriteAllText(privPath, priv);
        File.WriteAllText(pubPath, pub);
        Console.WriteLine($"private => {privPath}");
        Console.WriteLine($"public  => {pubPath}");
        Console.WriteLine();
        Console.WriteLine("Copy the public PEM into src/Ledger.Desktop/Infrastructure/VendorPublicKey.cs.");
        Console.WriteLine("Keep the private PEM in a vault. Never commit it.");
        return 0;
    }

    private static int Help()
    {
        Console.WriteLine("""
            Ledger.Vendor — license key issuance CLI

            Commands:
              issue   --private <pem> --device <id> --customer <name>
                      [--edition Standard] [--expires YYYY-MM-DD] [--reference TEXT]
              keygen  --out <dir>      Generates a new RSA-2048 vendor keypair.
              help                     Show this help.
            """);
        return 0;
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            result[key] = value;
        }
        return result;
    }
}
