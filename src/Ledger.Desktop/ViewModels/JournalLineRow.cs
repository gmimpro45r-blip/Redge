using CommunityToolkit.Mvvm.ComponentModel;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Edit-model for a single row of the Journal Entry DataGrid. Wraps the domain
/// <c>JournalLine</c>; the view-model translates it to the domain entity on save.
/// </summary>
public sealed partial class JournalLineRow : ObservableObject
{
    [ObservableProperty] private short _lineNo;
    [ObservableProperty] private Guid _accountId;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private decimal _debit;
    [ObservableProperty] private decimal _credit;
    [ObservableProperty] private Guid? _costCenterId;
}
