namespace Ledger.Core.Enums;

/// <summary>
/// Role of a local user. Drives the role-based authorisation checks across the app.
/// </summary>
public enum UserRole
{
    /// <summary>Full access including user management, COA edits, period close, license entry.</summary>
    Admin,

    /// <summary>Can create &amp; post journal entries; cannot manage users, COA, or licensing.</summary>
    Accountant,

    /// <summary>Can create draft entries only; cannot post; cannot view reports beyond their own entries.</summary>
    DataEntry,

    /// <summary>Read-only access to reports and the chart of accounts.</summary>
    Viewer,
}
