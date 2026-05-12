using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Core.Entities;
using Ledger.Data;
using Ledger.Data.Abstractions;
using Ledger.Services.Accounting;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Journal Entry editor. Lets the user pick accounts per line, enter debits/credits,
/// see live totals + balance indicator, then Save as Draft or Post via
/// <see cref="AccountingService"/>. Posting enforces Σdebit = Σcredit at the domain.
/// </summary>
public sealed partial class JournalEntryViewModel : ObservableObject
{
    private readonly LedgerDbContext _db;
    private readonly IJournalRepository _journals;
    private readonly AccountingService _accounting;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<JournalLineRow> Lines { get; } = new();
    public ObservableCollection<string> Currencies { get; } = new() { "USD", "EUR", "EGP", "SAR" };

    [ObservableProperty] private Guid? _entryId;
    [ObservableProperty] private string _entryNo = "(auto)";
    [ObservableProperty] private DateTime _entryDate = DateTime.Today;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private decimal _exchangeRate = 1m;
    [ObservableProperty] private string? _description;

    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;
    [ObservableProperty] private string _balanceStatus = "Empty";
    [ObservableProperty] private Brush _balanceStatusBrush = Brushes.Gray;
    [ObservableProperty] private bool _canPost;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public JournalEntryViewModel(
        LedgerDbContext db,
        IJournalRepository journals,
        AccountingService accounting)
    {
        _db = db;
        _journals = journals;
        _accounting = accounting;
        Lines.CollectionChanged += (_, __) => Recompute();
    }

    public async Task LoadAsync()
    {
        var rows = await _db.Accounts.OrderBy(a => a.Code).ToListAsync().ConfigureAwait(true);
        Accounts.Clear();
        foreach (var a in rows) Accounts.Add(a);
        if (Lines.Count == 0)
        {
            Lines.Add(new JournalLineRow { LineNo = 1 });
            Lines.Add(new JournalLineRow { LineNo = 2 });
        }
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new JournalLineRow { LineNo = (short)(Lines.Count + 1) });

    [RelayCommand]
    private void RemoveLine(JournalLineRow? row)
    {
        if (row is null) return;
        Lines.Remove(row);
        for (short i = 0; i < Lines.Count; i++) Lines[i].LineNo = (short)(i + 1);
    }

    private void Recompute()
    {
        foreach (var l in Lines)
        {
            l.PropertyChanged -= OnLineChanged;
            l.PropertyChanged += OnLineChanged;
        }
        TotalDebit = decimal.Round(Lines.Sum(l => l.Debit), 4);
        TotalCredit = decimal.Round(Lines.Sum(l => l.Credit), 4);
        if (TotalDebit == 0m && TotalCredit == 0m)
        {
            BalanceStatus = "Empty";
            BalanceStatusBrush = Brushes.Gray;
            CanPost = false;
        }
        else if (TotalDebit == TotalCredit)
        {
            BalanceStatus = $"Balanced ({TotalDebit:N4})";
            BalanceStatusBrush = Brushes.Green;
            CanPost = Lines.Count >= 2 && EntryId is null;
        }
        else
        {
            BalanceStatus = $"Unbalanced (diff {TotalDebit - TotalCredit:N4})";
            BalanceStatusBrush = Brushes.Red;
            CanPost = false;
        }
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs e) => Recompute();

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        StatusMessage = string.Empty;
        try
        {
            var validRows = Lines.Where(l => l.AccountId != Guid.Empty && (l.Debit > 0m || l.Credit > 0m)).ToList();
            if (validRows.Count == 0)
            {
                HasError = true;
                ErrorMessage = "Add at least one line with an account and amount.";
                return;
            }

            var entryNo = await _journals.NextEntryNoAsync(Guid.Empty).ConfigureAwait(true);
            var header = JournalHeader.CreateDraft(
                orgId: Guid.Empty,
                entryNo: entryNo,
                entryDate: DateOnly.FromDateTime(EntryDate),
                currency: Currency,
                exchangeRate: ExchangeRate,
                description: Description);
            foreach (var row in validRows)
            {
                header.AddLine(row.AccountId, row.Debit, row.Credit, row.Description);
            }

            var result = await _accounting.CreateDraftAsync(header).ConfigureAwait(true);
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Error ?? "Could not save the entry.";
                return;
            }

            EntryId = header.Id;
            EntryNo = header.EntryNo;
            StatusMessage = $"Saved draft {header.EntryNo}.";
            Recompute();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        if (IsBusy || EntryId is null) return;
        IsBusy = true;
        HasError = false;
        try
        {
            var result = await _accounting.PostAsync(EntryId.Value).ConfigureAwait(true);
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Error ?? "Posting failed.";
                return;
            }
            StatusMessage = $"Posted {EntryNo}.";
            CanPost = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewEntry()
    {
        EntryId = null;
        EntryNo = "(auto)";
        Description = null;
        ExchangeRate = 1m;
        Lines.Clear();
        Lines.Add(new JournalLineRow { LineNo = 1 });
        Lines.Add(new JournalLineRow { LineNo = 2 });
        StatusMessage = string.Empty;
        HasError = false;
        Recompute();
    }
}
