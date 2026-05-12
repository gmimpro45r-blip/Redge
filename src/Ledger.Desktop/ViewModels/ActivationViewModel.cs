using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Desktop.Infrastructure;
using Ledger.Services.Licensing;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// View-model behind <c>ActivationWindow.xaml</c>.
/// Displays the local device id, accepts an activation key, validates it against
/// the embedded vendor RSA public key, and on success persists a
/// <c>LicenseInfo</c> row into the encrypted database.
/// </summary>
public sealed partial class ActivationViewModel : ObservableObject
{
    private readonly IHardwareIdentifier _hardware;
    private readonly LicenseBootstrap _bootstrap;

    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _activationKey = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isActivated;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _edition = string.Empty;

    public ActivationViewModel(IHardwareIdentifier hardware, LicenseBootstrap bootstrap)
    {
        _hardware = hardware;
        _bootstrap = bootstrap;
        DeviceId = _hardware.GetDeviceId();
    }

    [RelayCommand]
    private void CopyDeviceId()
    {
        try
        {
            Clipboard.SetText(DeviceId);
        }
        catch
        {
            // Clipboard contention with another app — non-fatal.
        }
    }

    [RelayCommand]
    private async Task ActivateAsync(Window? window)
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var (ok, error, payload) = await _bootstrap
                .ActivateAsync(ActivationKey, DateOnly.FromDateTime(DateTime.UtcNow))
                .ConfigureAwait(true);

            if (!ok)
            {
                HasError = true;
                ErrorMessage = error switch
                {
                    "INVALID_SIGNATURE" => "The activation key is not signed by Ledger and was rejected.",
                    "DEVICE_MISMATCH" => "This key was issued for a different machine.",
                    "EXPIRED" => "This activation key has expired.",
                    null => "Activation failed.",
                    _ => $"Activation failed: {error}",
                };
                return;
            }

            CustomerName = payload?.CustomerName ?? string.Empty;
            Edition = payload?.Edition ?? string.Empty;
            IsActivated = true;
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
