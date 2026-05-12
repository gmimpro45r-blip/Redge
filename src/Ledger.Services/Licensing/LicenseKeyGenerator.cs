using System.Security.Cryptography;

namespace Ledger.Services.Licensing;

/// <summary>
/// VENDOR-SIDE: signs a <see cref="LicensePayload"/> with the private RSA key and
/// produces the activation key string the customer types into the Activation screen.
/// <para>
/// Never ship the private key with the app. Keep it in your vendor key-vault and use
/// this class only from your offline key-issuance tool.
/// </para>
/// </summary>
public sealed class LicenseKeyGenerator
{
    private readonly RSA _privateKey;

    public LicenseKeyGenerator(string privateKeyPem)
    {
        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(privateKeyPem);
    }

    public string Issue(LicensePayload payload)
    {
        var bytes = LicenseKeyCodec.SerializePayload(payload);
        var signature = _privateKey.SignData(
            bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return LicenseKeyCodec.Encode(bytes, signature);
    }

    /// <summary>Generates a fresh 2048-bit RSA key pair (run once per product release).</summary>
    public static (string PrivatePem, string PublicPem) CreateKeyPair(int keySizeInBits = 2048)
    {
        using var rsa = RSA.Create(keySizeInBits);
        var priv = rsa.ExportPkcs8PrivateKeyPem();
        var pub = rsa.ExportSubjectPublicKeyInfoPem();
        return (priv, pub);
    }
}
