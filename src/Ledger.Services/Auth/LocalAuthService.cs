using Ledger.Core.Abstractions;
using Ledger.Core.Entities;
using Ledger.Core.Enums;
using Ledger.Data;
using Ledger.Data.Abstractions;
using Ledger.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Services.Auth;

/// <summary>
/// Offline, local authentication. Verifies BCrypt-hashed passwords stored in the
/// encrypted SQLite database and updates lockout counters / last-login timestamps.
/// </summary>
public sealed class LocalAuthService
{
    private const int FailedAttemptThreshold = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly LedgerDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly UserSession _session;
    private readonly IClock _clock;

    public LocalAuthService(
        LedgerDbContext db,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        UserSession session,
        IClock clock)
    {
        _db = db;
        _uow = uow;
        _hasher = hasher;
        _session = session;
        _clock = clock;
    }

    public async Task<Result> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Result.Fail("CREDENTIALS_REQUIRED");
        }

        var normalised = username.Trim().ToLowerInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == normalised, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            return Result.Fail("INVALID_CREDENTIALS");
        }
        if (user.IsLocked(_clock.UtcNow))
        {
            return Result.Fail("ACCOUNT_LOCKED");
        }

        if (!_hasher.Verify(password, user.PasswordHash))
        {
            user.RegisterFailedLogin(_clock.UtcNow, FailedAttemptThreshold, LockDuration);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Fail("INVALID_CREDENTIALS");
        }

        user.RegisterSuccessfulLogin(_clock.UtcNow);
        _db.AuditLogs.Add(AuditLog.Record(
            action: "user.login",
            userId: user.Id,
            entityName: nameof(User),
            entityId: user.Id.ToString()));
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _session.SignIn(user.Id, orgId: Guid.Empty, user.Username, user.DisplayName, user.Role);
        return Result.Ok();
    }

    public Task<Result> SignOutAsync()
    {
        if (_session.IsAuthenticated)
        {
            _db.AuditLogs.Add(AuditLog.Record(
                action: "user.logout",
                userId: _session.UserId));
        }
        _session.SignOut();
        return Task.FromResult(Result.Ok());
    }

    public async Task<Result> RegisterAsync(
        string username,
        string displayName,
        string password,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == username.Trim().ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return Result.Fail("USERNAME_TAKEN");
        }

        var hash = _hasher.Hash(password);
        var user = User.Create(username, displayName, hash, role);
        _db.Users.Add(user);
        _db.AuditLogs.Add(AuditLog.Record("user.created", _session.UserId, nameof(User), user.Id.ToString(),
            details: $"username={user.Username};role={role}"));
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
