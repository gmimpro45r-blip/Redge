using System.IO;

namespace Ledger.Desktop.Infrastructure;

/// <summary>
/// Resolves and creates the directories used by the desktop app.
/// Default: <c>%LOCALAPPDATA%\Redge</c>. Overridable via the <c>LEDGER_DB_PATH</c>
/// environment variable for portable / OEM installs.
/// </summary>
public sealed class AppPaths
{
    public string AppDataDirectory { get; }
    public string DatabasePath { get; }
    public string LogDirectory { get; }
    public string SettingsPath { get; }

    public AppPaths()
    {
        var custom = Environment.GetEnvironmentVariable("LEDGER_DB_PATH");
        if (!string.IsNullOrWhiteSpace(custom))
        {
            DatabasePath = custom;
            AppDataDirectory = Path.GetDirectoryName(Path.GetFullPath(custom))!;
        }
        else
        {
            AppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Redge");
            DatabasePath = Path.Combine(AppDataDirectory, "ledger.db");
        }

        LogDirectory = Path.Combine(AppDataDirectory, "logs");
        SettingsPath = Path.Combine(AppDataDirectory, "settings.json");

        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
