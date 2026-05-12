using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Core.Enums;
using Ledger.Services.Auth;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// First-run wizard: creates the initial Admin user before any login screen appears.
/// Runs only when the encrypted database has zero <c>Users</c> rows.
/// </summary>
public sealed partial class FirstRunViewModel : ObservableObject
{
    private readonly LocalAuthService _auth;

    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _displayName = "Administrator";
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _passwordConfirm = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _completed;

    public FirstRunViewModel(LocalAuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task CreateAsync(Window? window)
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        try
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(DisplayName))
            {
                HasError = true;
                ErrorMessage = "Username and display name are required.";
                return;
            }
            if (Password.Length < 8)
            {
                HasError = true;
                ErrorMessage = "Password must be at least 8 characters.";
                return;
            }
            if (Password != PasswordConfirm)
            {
                HasError = true;
                ErrorMessage = "Passwords do not match.";
                return;
            }

            var result = await _auth.RegisterAsync(Username, DisplayName, Password, UserRole.Admin)
                .ConfigureAwait(true);
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Error ?? "Failed to create the user.";
                return;
            }

            Completed = true;
            if (window is not null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
