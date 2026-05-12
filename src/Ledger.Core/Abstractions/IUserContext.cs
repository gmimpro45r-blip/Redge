namespace Ledger.Core.Abstractions;

/// <summary>
/// Identity of the caller. Injected by the host (WPF app or unit tests).
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }
    Guid OrgId { get; }
    string Role { get; }
}
