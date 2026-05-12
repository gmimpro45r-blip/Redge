using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Services.Auth;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Top-level shell view-model that drives <c>MainWindow</c>'s navigation drawer
/// and content host. Switches the <see cref="CurrentView"/> between dashboard,
/// chart of accounts, and journal entry.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly UserSession _session;
    private readonly DashboardViewModel _dashboard;
    private readonly ChartOfAccountsViewModel _accounts;
    private readonly JournalEntryViewModel _journal;

    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _currentTitle = "Dashboard";

    public string Username => _session.IsAuthenticated ? _session.DisplayName : "(not signed in)";
    public string Role => _session.IsAuthenticated ? _session.Role.ToString() : string.Empty;

    public ShellViewModel(
        UserSession session,
        DashboardViewModel dashboard,
        ChartOfAccountsViewModel accounts,
        JournalEntryViewModel journal)
    {
        _session = session;
        _dashboard = dashboard;
        _accounts = accounts;
        _journal = journal;
        _ = ShowDashboardAsync();
    }

    [RelayCommand]
    public async Task ShowDashboardAsync()
    {
        await _dashboard.RefreshAsync().ConfigureAwait(true);
        CurrentView = _dashboard;
        CurrentTitle = "Dashboard";
    }

    [RelayCommand]
    public async Task ShowAccountsAsync()
    {
        await _accounts.RefreshAsync().ConfigureAwait(true);
        CurrentView = _accounts;
        CurrentTitle = "Chart of Accounts";
    }

    [RelayCommand]
    public async Task ShowJournalAsync()
    {
        await _journal.LoadAsync().ConfigureAwait(true);
        CurrentView = _journal;
        CurrentTitle = "Journal Entry";
    }
}
