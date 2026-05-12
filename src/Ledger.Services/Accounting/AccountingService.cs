using Ledger.Core.Abstractions;
using Ledger.Core.Entities;
using Ledger.Data.Abstractions;
using Ledger.Shared;

namespace Ledger.Services.Accounting;

/// <summary>
/// Orchestrates the core accounting use-cases: drafting, posting, and reversing entries.
/// All persistence is delegated to <see cref="IJournalRepository"/> / <see cref="IUnitOfWork"/>;
/// this class contains the application-level coordination only.
/// </summary>
public sealed class AccountingService
{
    private readonly IJournalRepository _journals;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IUserContext _user;

    public AccountingService(
        IJournalRepository journals,
        IUnitOfWork uow,
        IClock clock,
        IUserContext user)
    {
        _journals = journals;
        _uow = uow;
        _clock = clock;
        _user = user;
    }

    public async Task<Result<Guid>> CreateDraftAsync(
        JournalHeader draft,
        CancellationToken cancellationToken = default)
    {
        if (draft.Lines.Count == 0)
        {
            return Result<Guid>.Fail("EMPTY_ENTRY");
        }

        await _journals.AddAsync(draft, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Ok(draft.Id);
    }

    public async Task<Result> PostAsync(Guid headerId, CancellationToken cancellationToken = default)
    {
        var header = await _journals.GetWithLinesAsync(headerId, cancellationToken).ConfigureAwait(false);
        if (header is null)
        {
            return Result.Fail($"NOT_FOUND: {headerId}");
        }

        var result = header.Post(_clock, _user.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        _journals.Update(header);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<Guid>> ReverseAsync(
        Guid headerId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var header = await _journals.GetWithLinesAsync(headerId, cancellationToken).ConfigureAwait(false);
        if (header is null)
        {
            return Result<Guid>.Fail($"NOT_FOUND: {headerId}");
        }

        var entryNo = await _journals.NextEntryNoAsync(header.OrgId, cancellationToken).ConfigureAwait(false);

        try
        {
            var reversal = header.CreateReversal(_clock, _user.UserId, entryNo, reason);
            await _journals.AddAsync(reversal, cancellationToken).ConfigureAwait(false);
            _journals.Update(header);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<Guid>.Ok(reversal.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Fail(ex.Message);
        }
    }
}
