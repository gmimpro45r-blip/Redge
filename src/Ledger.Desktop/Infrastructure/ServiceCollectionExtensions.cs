using System.Runtime.Versioning;
using Ledger.Core.Abstractions;
using Ledger.Data;
using Ledger.Data.Abstractions;
using Ledger.Data.Repositories;
using Ledger.Data.Sqlite;
using Ledger.Desktop.Licensing;
using Ledger.Desktop.ViewModels;
using Ledger.Desktop.Views;
using Ledger.Services.Accounting;
using Ledger.Services.Auth;
using Ledger.Services.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// Wires up every dependency the WPF host needs into a single DI container.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLedger(this IServiceCollection services)
    {
        services.AddSingleton<AppPaths>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IHardwareIdentifier, WmiHardwareIdentifier>();
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserContext>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddSingleton<LicenseValidator>(sp =>
            new LicenseValidator(VendorPublicKey.Pem, sp.GetRequiredService<IHardwareIdentifier>()));

        services.AddSingleton<DatabaseKeyProvider>(sp =>
            new DatabaseKeyProvider(
                sp.GetRequiredService<AppPaths>().AppDataDirectory,
                sp.GetRequiredService<IHardwareIdentifier>()));

        services.AddSingleton<SqlCipherConnectionFactory>(sp =>
        {
            var paths = sp.GetRequiredService<AppPaths>();
            var key = sp.GetRequiredService<DatabaseKeyProvider>().GetOrCreateKey();
            return new SqlCipherConnectionFactory(paths.DatabasePath, key);
        });

        services.AddDbContext<LedgerDbContext>((sp, options) =>
        {
            var factory = sp.GetRequiredService<SqlCipherConnectionFactory>();
            var conn = factory.OpenConnection();
            options.UseSqlite(conn);
        }, ServiceLifetime.Transient);

        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient<IAccountRepository, AccountRepository>();
        services.AddTransient<IJournalRepository, JournalRepository>();

        services.AddTransient<AccountingService>();
        services.AddTransient<LocalAuthService>();
        services.AddTransient<DatabaseBootstrap>();
        services.AddTransient<LicenseBootstrap>();

        services.AddTransient<ActivationViewModel>();
        services.AddTransient<FirstRunViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ChartOfAccountsViewModel>();
        services.AddTransient<JournalEntryViewModel>();
        services.AddTransient<ShellViewModel>();

        services.AddTransient<MainWindow>();

        return services;
    }
}
