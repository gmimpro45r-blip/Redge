using FluentAssertions;
using Ledger.Services.Licensing;

namespace Ledger.Services.Tests.Licensing;

public class LicenseValidatorTests
{
    private const string Device = "abc123deadbeef";
    private static readonly DateOnly Today = new(2026, 5, 12);

    private static (LicenseKeyGenerator gen, string publicPem) CreatePair()
    {
        var (privPem, pubPem) = LicenseKeyGenerator.CreateKeyPair(2048);
        return (new LicenseKeyGenerator(privPem), pubPem);
    }

    private static LicensePayload Payload(string deviceId, DateOnly? expires = null) => new()
    {
        DeviceId = deviceId,
        CustomerName = "Acme Inc.",
        Edition = "Standard",
        IssuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        ExpiresAt = expires,
        Reference = "INV-001",
    };

    [Fact]
    public void Valid_key_is_accepted()
    {
        var (gen, pub) = CreatePair();
        var key = gen.Issue(Payload(Device, expires: new DateOnly(2027, 1, 1)));
        var v = new LicenseValidator(pub, new FakeHardwareIdentifier(Device));

        var result = v.Validate(key, Today);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerName.Should().Be("Acme Inc.");
        result.Value.ExpiresAt.Should().Be(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void Perpetual_key_no_expiry_is_accepted()
    {
        var (gen, pub) = CreatePair();
        var key = gen.Issue(Payload(Device, expires: null));
        var v = new LicenseValidator(pub, new FakeHardwareIdentifier(Device));

        v.Validate(key, Today).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Expired_key_is_rejected()
    {
        var (gen, pub) = CreatePair();
        var key = gen.Issue(Payload(Device, expires: new DateOnly(2025, 1, 1)));
        var v = new LicenseValidator(pub, new FakeHardwareIdentifier(Device));

        var result = v.Validate(key, Today);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("EXPIRED");
    }

    [Fact]
    public void Wrong_device_is_rejected()
    {
        var (gen, pub) = CreatePair();
        var key = gen.Issue(Payload(Device));
        var v = new LicenseValidator(pub, new FakeHardwareIdentifier("DIFFERENT-MACHINE"));

        var result = v.Validate(key, Today);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("DEVICE_MISMATCH");
    }

    [Fact]
    public void Tampered_payload_fails_signature_check()
    {
        var (gen, pub) = CreatePair();
        var key = gen.Issue(Payload(Device));

        // Flip a character in the payload portion (before the '.') — signature should fail.
        var parts = key.Split('.');
        var tampered = parts[0][..^1] + (parts[0][^1] == 'A' ? 'B' : 'A') + "." + parts[1];

        var v = new LicenseValidator(pub, new FakeHardwareIdentifier(Device));
        var result = v.Validate(tampered, Today);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("INVALID_SIGNATURE");
    }

    [Fact]
    public void Key_signed_by_wrong_private_key_is_rejected()
    {
        var (gen,  _)   = CreatePair();              // attacker's pair
        var ( _,  pub2) = CreatePair();              // vendor's public key
        var key = gen.Issue(Payload(Device));

        var v = new LicenseValidator(pub2, new FakeHardwareIdentifier(Device));
        v.Validate(key, Today).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Malformed_key_is_rejected()
    {
        var (_, pub) = CreatePair();
        var v = new LicenseValidator(pub, new FakeHardwareIdentifier(Device));

        v.Validate("not-a-valid-key", Today).IsFailure.Should().BeTrue();
        v.Validate("",                 Today).IsFailure.Should().BeTrue();
        v.Validate("aaa.bbb",          Today).IsFailure.Should().BeTrue();
    }
}
