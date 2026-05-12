namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// RSA-2048 vendor public key embedded in the app. The matching <b>private</b> key
/// is held by the vendor (Michael) and is used by
/// <c>Ledger.Services.Licensing.LicenseKeyGenerator</c> to sign activation keys.
/// <para/>
/// <b>This is a DEVELOPMENT key.</b> The matching dev private key lives under
/// <c>dev/keys/vendor_private.pem</c> so the vendor CLI tool
/// (<c>tools/Ledger.Vendor</c>) can issue activation keys for local testing.
/// Before shipping a commercial release, replace the PEM below with one generated
/// by <c>LicenseKeyGenerator.CreateKeyPair(2048)</c> and store the matching
/// production private key in a vault — never commit it.
/// </summary>
public static class VendorPublicKey
{
    public const string Pem = """
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAza4T++STHEtY3OXWApmi
4ksyvxsWi8XEEg9ClXfLGQ3+ANJvsB3lfw7bS71BPYqO7anrQOm7SqnVQmMqIWVl
j/RR8Gwwfx0U5eFhhX2ISAZOfhBTMrtEffkrENvk3M6dT4Zq84DGcR3MKyJIYn9a
SFlAKeq9w3ZYr1JWOZDIgwu8welstwKHgpJM9nJq7IX51xOzJf3Ql/CwTO77w2yv
l0ZejzPKcAC8suzBpvgk+crnS4GByVY9sPi9Oz2GdjYVtG/dYweRa2exi92h3VdQ
2gecs6+hPVUKcQJ+32Cx8aBSIJ4YQBemdfS6DPAVzd7LkJh/lL33upsU/610iv/2
IQIDAQAB
-----END PUBLIC KEY-----
""";
}
