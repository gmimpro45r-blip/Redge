using FluentAssertions;
using Ledger.Services.Integration;

namespace Ledger.Services.Tests;

public class N8nWebhookSignerTests
{
    private const string Secret = "test-secret-do-not-use-in-prod";
    private const string Body = "{\"event\":\"journal.posted\",\"id\":\"abc-123\"}";
    private static readonly DateTimeOffset Now = new(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Sign_returns_lowercase_hex_with_sha256_prefix()
    {
        var sig = N8nWebhookSigner.Sign(Body, Now.ToUnixTimeSeconds(), Secret);
        sig.Should().StartWith("sha256=");
        sig.Substring(7).Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Verify_accepts_valid_signature_within_replay_window()
    {
        var ts = Now.ToUnixTimeSeconds();
        var sig = N8nWebhookSigner.Sign(Body, ts, Secret);
        N8nWebhookSigner.Verify(Body, ts, sig, Secret, Now).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_tampered_body()
    {
        var ts = Now.ToUnixTimeSeconds();
        var sig = N8nWebhookSigner.Sign(Body, ts, Secret);
        N8nWebhookSigner.Verify(Body + "!", ts, sig, Secret, Now).Should().BeFalse();
    }

    [Fact]
    public void Verify_rejects_signature_after_replay_window()
    {
        var ts = Now.ToUnixTimeSeconds();
        var sig = N8nWebhookSigner.Sign(Body, ts, Secret);
        var sixMinutesLater = Now.AddMinutes(6);
        N8nWebhookSigner.Verify(Body, ts, sig, Secret, sixMinutesLater).Should().BeFalse();
    }

    [Fact]
    public void Verify_rejects_wrong_secret()
    {
        var ts = Now.ToUnixTimeSeconds();
        var sig = N8nWebhookSigner.Sign(Body, ts, Secret);
        N8nWebhookSigner.Verify(Body, ts, sig, "other-secret", Now).Should().BeFalse();
    }
}
