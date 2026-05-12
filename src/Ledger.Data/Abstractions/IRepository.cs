using Ledger.Core.Entities;

namespace Ledger.Data.Abstractions;

/// <summary>
/// Minimal repository contract. Specific repositories add domain-specific queries.
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
}

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByCodeAsync(Guid orgId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> ListByOrgAsync(Guid orgId, CancellationToken cancellationToken = default);
}

public interface IJournalRepository : IRepository<JournalHeader>
{
    Task<JournalHeader?> GetWithLinesAsync(Guid headerId, CancellationToken cancellationToken = default);
    Task<string> NextEntryNoAsync(Guid orgId, CancellationToken cancellationToken = default);
}
