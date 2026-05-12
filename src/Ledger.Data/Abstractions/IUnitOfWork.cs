namespace Ledger.Data.Abstractions;

/// <summary>
/// Coordinates a transactional write across multiple repositories.
/// Milestone 3 will provide an EF Core-backed implementation.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
