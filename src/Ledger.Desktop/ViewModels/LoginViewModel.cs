using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Services.Auth;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// Login dialog backing model. Verifies BCrypt-hashed passwords via
/// <see cref="LocalAuthService"/>; on success flips <see cref="SignedIn"/> and closes
/// the window.
/// </summary>
public sealed partial class LoginViewModel : ObservableObject
{
    private readonly LocalAuthService _auth;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _signedIn;

    public LoginViewModel(LocalAuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task SignInAsync(Window? window)
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        try
        {
            var result = await _auth.SignInAsync(Username, Password).ConfigureAwait(true);
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Error switch
                {
                    "INVALID_CREDENTIALS" => "Invalid username or password.",
                    "ACCOUNT_LOCKED" => "This account is temporarily locked. Try again later.",
                    "CREDENTIALS_REQUIRED" => "Please enter your username and password.",
                    _ => result.Error ?? "Sign-in failed.",
                };
                return;
            }

            SignedIn = true;
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
