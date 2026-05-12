using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using Ledger.Desktop.Infrastructure;
using Ledger.Desktop.ViewModels;
using Ledger.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Ledger.Desktop;

/// <summary>
/// WPF application entry point. Builds a generic host with DI + Serilog,
/// then orchestrates the startup flow: DB ready → license check → login → shell.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class App : Application
{
    public static IHost Host { get; private set; } = default!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetCulture();

        var paths = new AppPaths();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(paths.LogDirectory, "ledger-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        try
        {
            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton(paths);
                    services.AddLedger();
                })
                .Build();

            await Host.StartAsync().ConfigureAwait(true);

            await using (var scope = Host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<DatabaseBootstrap>()
                    .EnsureReadyAsync()
                    .ConfigureAwait(true);
            }

            if (!await RunActivationAsync().ConfigureAwait(true))
            {
                Shutdown(0);
                return;
            }

            if (!await RunAuthAsync().ConfigureAwait(true))
            {
                Shutdown(0);
                return;
            }

            var shell = Host.Services.GetRequiredService<MainWindow>();
            shell.DataContext = Host.Services.GetRequiredService<ShellViewModel>();
            shell.Show();
            MainWindow = shell;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            MessageBox.Show(
                $"Application could not start:\n\n{ex.Message}",
                "Ledger",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static async Task<bool> RunActivationAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<LicenseBootstrap>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await bootstrap.GetCurrentAsync(today).ConfigureAwait(true);
        if (existing is not null) return true;

        var vm = scope.ServiceProvider.GetRequiredService<ActivationViewModel>();
        var window = new ActivationWindow { DataContext = vm };
        var result = window.ShowDialog();
        return result == true && vm.IsActivated;
    }

    private static async Task<bool> RunAuthAsync()
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Ledger.Data.LedgerDbContext>();
        var hasAnyUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .AnyAsync(db.Users)
            .ConfigureAwait(true);

        if (!hasAnyUser)
        {
            var vm = scope.ServiceProvider.GetRequiredService<FirstRunViewModel>();
            var window = new FirstRunWindow { DataContext = vm };
            if (window.ShowDialog() != true) return false;
        }

        await using var loginScope = Host.Services.CreateAsyncScope();
        var loginVm = loginScope.ServiceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow { DataContext = loginVm };
        return loginWindow.ShowDialog() == true && loginVm.SignedIn;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Host is not null)
        {
            await Host.StopAsync().ConfigureAwait(true);
            Host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void SetCulture()
    {
        var culture = Environment.GetEnvironmentVariable("LEDGER_DEFAULT_FLOW") == "RightToLeft"
            ? new CultureInfo("ar-EG")
            : CultureInfo.CurrentUICulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
