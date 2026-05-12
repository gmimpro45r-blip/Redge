using FluentAssertions;
using Ledger.Services.Auth;

namespace Ledger.Services.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_returns_different_hashes_for_same_input()
    {
        var a = _hasher.Hash("Sup3rSecret!");
        var b = _hasher.Hash("Sup3rSecret!");
        a.Should().NotBe(b);                       // BCrypt salt differs each call
        a.Should().StartWith("$2");                // BCrypt prefix
    }

    [Fact]
    public void Verify_succeeds_with_correct_password()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");
        _hasher.Verify("CorrectHorseBatteryStaple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_fails_with_wrong_password()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");
        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "h")]
    [InlineData("p",   null)]
    [InlineData("",    "")]
    public void Verify_handles_empty_inputs_gracefully(string? plain, string? hash)
    {
        _hasher.Verify(plain!, hash!).Should().BeFalse();
    }

    [Fact]
    public void Hash_rejects_empty_password()
    {
        var act = () => _hasher.Hash("");
        act.Should().Throw<ArgumentException>();
    }
}
