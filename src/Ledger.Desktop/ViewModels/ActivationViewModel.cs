using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ledger.Services.Licensing;

namespace Ledger.Desktop.ViewModels;

/// <summary>
/// View-model behind <c>ActivationWindow.xaml</c>.
/// Displays the local device id, accepts a key, and reports validation errors.
/// </summary>
public sealed partial class ActivationViewModel : ObservableObject
{
    private readonly IHardwareIdentifier _hardware;
    private readonly LicenseValidator _validator;

    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _activationKey = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isActivated;

    public ActivationViewModel(IHardwareIdentifier hardware, LicenseValidator validator)
    {
        _hardware = hardware;
        _validator = validator;
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
    private void Activate()
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = _validator.Validate(ActivationKey.Trim(), DateOnly.FromDateTime(DateTime.UtcNow));
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = $"Activation failed: {result.Error}";
                return;
            }
            IsActivated = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
