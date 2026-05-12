using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Minimal welcome dashboard: shows headline counts so the user has visible proof
/// the encrypted database is up and reading. M4 expands this with LiveCharts2.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly LedgerDbContext _db;

    [ObservableProperty] private int _accountCount;
    [ObservableProperty] private int _journalCount;
    [ObservableProperty] private int _postedCount;
    [ObservableProperty] private int _userCount;

    public DashboardViewModel(LedgerDbContext db)
    {
        _db = db;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        AccountCount = await _db.Accounts.CountAsync().ConfigureAwait(true);
        JournalCount = await _db.JournalHeaders.CountAsync().ConfigureAwait(true);
        PostedCount = await _db.JournalHeaders
            .Where(h => h.Status == Ledger.Core.Enums.EntryStatus.Posted)
            .CountAsync()
            .ConfigureAwait(true);
        UserCount = await _db.Users.CountAsync().ConfigureAwait(true);
    }
}
