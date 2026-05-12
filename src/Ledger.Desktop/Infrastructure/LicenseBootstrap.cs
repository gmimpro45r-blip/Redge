using Ledger.Core.Entities;
using Ledger.Data;
using Ledger.Data.Abstractions;
using Ledger.Services.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// Reads / writes the single <see cref="LicenseInfo"/> row that tracks the current
/// device's activation status. Use <see cref="GetCurrentAsync"/> to gate the app on
/// startup; if it returns null, show the activation window and call
/// <see cref="ActivateAsync"/> with the user-entered key.
/// </summary>
public sealed class LicenseBootstrap
{
    private readonly LedgerDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly LicenseValidator _validator;
    private readonly IHardwareIdentifier _hardware;

    public LicenseBootstrap(
        LedgerDbContext db,
        IUnitOfWork uow,
        LicenseValidator validator,
        IHardwareIdentifier hardware)
    {
        _db = db;
        _uow = uow;
        _validator = validator;
        _hardware = hardware;
    }

    public async Task<LicenseInfo?> GetCurrentAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var info = await _db.Licenses
            .OrderByDescending(l => l.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (info is null) return null;
        if (!info.IsCurrentlyValid(today)) return null;

        var revalidate = _validator.Validate(info.ActivationKey, today);
        return revalidate.IsSuccess ? info : null;
    }

    public async Task<(bool ok, string? error, LicensePayload? payload)> ActivateAsync(
        string activationKey,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var result = _validator.Validate(activationKey.Trim(), today);
        if (result.IsFailure)
        {
            return (false, result.Error, null);
        }

        var payload = result.Value!;
        var deviceId = _hardware.GetDeviceId();
        var info = LicenseInfo.Create(
            deviceId: deviceId,
            activationKey: activationKey.Trim(),
            customerName: payload.CustomerName,
            edition: payload.Edition,
            expiresAt: payload.ExpiresAt);

        _db.Licenses.Add(info);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (true, null, payload);
    }
}
