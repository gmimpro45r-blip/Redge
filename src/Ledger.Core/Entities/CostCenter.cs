using Ledger.Shared;

namespace Ledger.Core.Entities;

public sealed class CostCenter : Entity
{
    public Guid OrgId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CostCenter() { }

    public static CostCenter Create(Guid orgId, string code, string name, Guid? parentId = null) => new()
    {
        OrgId = orgId,
        Code = Guard.NotNullOrWhiteSpace(code),
        Name = Guard.NotNullOrWhiteSpace(name),
        ParentId = parentId,
    };

    public void Deactivate() => IsActive = false;
}
