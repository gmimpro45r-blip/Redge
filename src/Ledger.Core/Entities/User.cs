using Ledger.Core.Enums;
using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// Local user record. Passwords are stored as BCrypt hashes (never plain text);
/// see <c>PasswordHasher</c> in <c>Ledger.Services</c>.
/// </summary>
public sealed class User : Entity
{
    public string Username { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; } = UserRole.DataEntry;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    private User() { }

    public static User Create(string username, string displayName, string passwordHash, UserRole role)
    {
        return new User
        {
            Username = Guard.NotNullOrWhiteSpace(username).Trim().ToLowerInvariant(),
            DisplayName = Guard.NotNullOrWhiteSpace(displayName),
            PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash),
            Role = role,
        };
    }

    public void ChangePassword(string newHash)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(newHash);
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset at)
    {
        LastLoginAt = at;
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void RegisterFailedLogin(DateTimeOffset at, int lockThreshold, TimeSpan lockDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= lockThreshold)
        {
            LockedUntil = at + lockDuration;
        }
    }

    public bool IsLocked(DateTimeOffset at) => LockedUntil.HasValue && LockedUntil.Value > at;

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;

    public void ChangeRole(UserRole role) => Role = role;
    public void Rename(string displayName) => DisplayName = Guard.NotNullOrWhiteSpace(displayName);
}
