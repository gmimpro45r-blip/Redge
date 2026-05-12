using Ledger.Core.Enums;
using Ledger.Shared;

namespace Ledger.Core.Entities;

/// <summary>
/// A node in the recursive Chart of Accounts tree.
/// Header rows are not postable; only leaf accounts accept journal lines.
/// </summary>
public sealed class Account : Entity
{
    public Guid OrgId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public AccountType AccountType { get; private set; }
    public NormalBalance NormalBalance { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsPostable { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    private Account() { }

    public static Account Create(
        Guid orgId,
        string code,
        string name,
        AccountType accountType,
        NormalBalance normalBalance,
        string currency,
        Guid? parentId = null,
        bool isPostable = true,
        string? nameAr = null)
    {
        return new Account
        {
            OrgId = orgId,
            Code = Guard.NotNullOrWhiteSpace(code),
            Name = Guard.NotNullOrWhiteSpace(name),
            NameAr = nameAr,
            AccountType = accountType,
            NormalBalance = normalBalance,
            Currency = Guard.NotNullOrWhiteSpace(currency),
            ParentId = parentId,
            IsPostable = isPostable,
            IsActive = true,
        };
    }

    public void Rename(string name, string? nameAr = null)
    {
        Name = Guard.NotNullOrWhiteSpace(name);
        NameAr = nameAr;
    }

    public void Deactivate() => IsActive = false;
}
