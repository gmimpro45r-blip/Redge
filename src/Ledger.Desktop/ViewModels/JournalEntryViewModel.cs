using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// View-model for the Journal Entry screen. Tracks live debit/credit totals,
/// surfaces a balance indicator, and exposes Save/Post/Reverse commands.
/// Persistence is wired up in M3 via <c>AccountingService</c>.
/// </summary>
public sealed partial class JournalEntryViewModel : ObservableObject
{
    public ObservableCollection<JournalLineRow> Lines { get; } = new();
    public ObservableCollection<string> Currencies { get; } = new() { "USD", "EUR", "EGP", "SAR" };

    [ObservableProperty] private string _entryNo = "JE-NEW";
    [ObservableProperty] private DateTime _entryDate = DateTime.Today;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private decimal _exchangeRate = 1m;
    [ObservableProperty] private string? _description;

    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;
    [ObservableProperty] private string _balanceStatus = "Empty";
    [ObservableProperty] private Brush _balanceStatusBrush = Brushes.Gray;
    [ObservableProperty] private bool _canPost;
    [ObservableProperty] private bool _canReverse;
    [ObservableProperty] private FlowDirection _flowDirection = FlowDirection.LeftToRight;

    public JournalEntryViewModel()
    {
        Lines.CollectionChanged += (_, __) => Recompute();
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

        if (Lines.Count == 0)
        {
            BalanceStatus = "Empty";
            BalanceStatusBrush = Brushes.Gray;
            CanPost = false;
            return;
        }
        if (TotalDebit == TotalCredit && TotalDebit > 0m)
        {
            BalanceStatus = "Balanced";
            BalanceStatusBrush = Brushes.Green;
            CanPost = true;
        }
        else
        {
            BalanceStatus = $"Unbalanced ({TotalDebit - TotalCredit:N4})";
            BalanceStatusBrush = Brushes.Red;
            CanPost = false;
        }
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs e) => Recompute();

    [RelayCommand]
    private void SaveDraft()
    {
        /* M3: persist via AccountingService.CreateDraftAsync */
    }

    [RelayCommand(CanExecute = nameof(CanPost))]
    private void Post()
    {
        /* M3: AccountingService.PostAsync */
    }

    [RelayCommand(CanExecute = nameof(CanReverse))]
    private void Reverse()
    {
        /* M3: AccountingService.ReverseAsync */
    }
}
