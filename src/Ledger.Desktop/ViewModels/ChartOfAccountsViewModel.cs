using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Core.Entities;
using Ledger.Core.Enums;
using Ledger.Data;
using Ledger.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Chart of Accounts: list view + create-account form. Multi-currency, supports
/// Arabic name field, validates uniqueness of <c>(OrgId, Code)</c> at the DB layer.
/// </summary>
public sealed partial class ChartOfAccountsViewModel : ObservableObject
{
    private readonly LedgerDbContext _db;
    private readonly IUnitOfWork _uow;

    public ObservableCollection<Account> Accounts { get; } = new();

    public AccountType[] AccountTypes { get; } =
        new[] { AccountType.Asset, AccountType.Liability, AccountType.Equity, AccountType.Revenue, AccountType.Expense };
    public NormalBalance[] NormalBalances { get; } =
        new[] { NormalBalance.Debit, NormalBalance.Credit };
    public string[] Currencies { get; } = new[] { "USD", "EUR", "EGP", "SAR", "AED", "GBP" };

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _nameAr = string.Empty;
    [ObservableProperty] private AccountType _accountType = AccountType.Asset;
    [ObservableProperty] private NormalBalance _normalBalance = NormalBalance.Debit;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;

    public ChartOfAccountsViewModel(LedgerDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var rows = await _db.Accounts.OrderBy(a => a.Code).ToListAsync().ConfigureAwait(true);
        Accounts.Clear();
        foreach (var a in rows) Accounts.Add(a);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        try
        {
            if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
            {
                HasError = true;
                ErrorMessage = "Code and Name are required.";
                return;
            }

            var account = Account.Create(
                orgId: Guid.Empty,
                code: Code.Trim(),
                name: Name.Trim(),
                accountType: AccountType,
                normalBalance: NormalBalance,
                currency: Currency,
                nameAr: string.IsNullOrWhiteSpace(NameAr) ? null : NameAr.Trim());
            _db.Accounts.Add(account);

            try
            {
                await _uow.SaveChangesAsync().ConfigureAwait(true);
            }
            catch (DbUpdateException ex)
            {
                HasError = true;
                ErrorMessage = $"Could not save account: {ex.InnerException?.Message ?? ex.Message}";
                return;
            }

            Code = string.Empty;
            Name = string.Empty;
            NameAr = string.Empty;
            await RefreshAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
