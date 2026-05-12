using System.Globalization;
using System.Windows;

namespace Ledger.Desktop;

/// <summary>
/// WPF application entry point. Wires up culture, theme and (in M3) DI / hosting.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Default culture from environment; the user can flip it from Settings later.
        var culture = Environment.GetEnvironmentVariable("LEDGER_DEFAULT_FLOW") == "RightToLeft"
            ? new CultureInfo("ar-EG")
            : CultureInfo.CurrentUICulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        base.OnStartup(e);
    }
}
