using Ledger.Core.Abstractions;
using Ledger.Core.Enums;

namespace Ledger.Services.Auth;

/// <summary>
/// Mutable representation of the currently logged-in user. The WPF host wires this up
/// as a singleton and replaces its fields on login / logout. Also satisfies
/// <see cref="IUserContext"/> so domain code can read who is acting.
/// </summary>
public sealed class UserSession : IUserContext
{
    public Guid UserId { get; private set; }
    public Guid OrgId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Viewer;
    public bool IsAuthenticated { get; private set; }

    string IUserContext.Role => Role.ToString();

    public void SignIn(Guid userId, Guid orgId, string username, string displayName, UserRole role)
    {
        UserId = userId;
        OrgId = orgId;
        Username = username;
        DisplayName = displayName;
        Role = role;
        IsAuthenticated = true;
    }

    public void SignOut()
    {
        UserId = Guid.Empty;
        OrgId = Guid.Empty;
        Username = string.Empty;
        DisplayName = string.Empty;
        Role = UserRole.Viewer;
        IsAuthenticated = false;
    }

    public bool HasRole(params UserRole[] allowed) => IsAuthenticated && allowed.Contains(Role);
}
