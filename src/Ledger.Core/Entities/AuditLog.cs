using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// Append-only record of significant actions. Inserted by services (login, post, reverse,
/// activate, change role, ...). The database guarantees immutability via triggers.
/// </summary>
public sealed class AuditLog : Entity
{
    public DateTimeOffset At { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? EntityName { get; private set; }
    public string? EntityId { get; private set; }
    public string? Details { get; private set; }   // JSON or free-text

    private AuditLog() { }

    public static AuditLog Record(
        string action,
        Guid? userId = null,
        string? entityName = null,
        string? entityId = null,
        string? details = null)
    {
        return new AuditLog
        {
            Action = Guard.NotNullOrWhiteSpace(action),
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
        };
    }
}
