# Redge — Architecture

> **Stack**: .NET 8 WPF · MVVM Toolkit · MaterialDesignInXaml · **SQLCipher-encrypted SQLite** · EF Core · **Hardware-locked RSA-signed licensing** · BCrypt local auth · QuestPDF · LiveCharts2 · ClosedXML · **Inno Setup** single-file EXE.

This is a **100 % offline, commercially-resellable** desktop accounting product. No cloud, no telemetry. Every install runs against a single encrypted database file on the customer's machine.

---

## 1. Solution layout

```
Accounting.sln
│
├── src/
│   ├── Ledger.Core/              ← Pure domain layer (no I/O)
│   │   ├── Entities/             ← Account, JournalHeader/Line, CostCenter, Currency,
│   │   │                            ExchangeRate, FiscalYear/Period, User, LicenseInfo,
│   │   │                            AuditLog
│   │   ├── Enums/                ← AccountType, NormalBalance, EntryStatus, FiscalStatus,
│   │   │                            UserRole
│   │   ├── ValueObjects/         ← Money (decimal + currency, 4dp)
│   │   └── Abstractions/         ← IClock, IUserContext
│   │
│   ├── Ledger.Shared/            ← Result<T>, Guard, cross-cutting helpers
│   │
│   ├── Ledger.Data/              ← EF Core + SQLite + SQLCipher
│   │   ├── LedgerDbContext.cs
│   │   ├── Sqlite/               ← SqlCipherInitializer, SqlCipherConnectionFactory
│   │   ├── Configurations/       ← IEntityTypeConfiguration<T> per entity
│   │   ├── Repositories/         ← AccountRepository, JournalRepository
│   │   ├── UnitOfWork.cs
│   │   └── Migrations/           ← (added in M3 — `dotnet ef migrations add InitialCreate`)
│   │
│   ├── Ledger.Services/          ← Application layer (no UI deps)
│   │   ├── Accounting/           ← AccountingService (draft / post / reverse)
│   │   ├── Auth/                 ← PasswordHasher (BCrypt), LocalAuthService, UserSession
│   │   └── Licensing/            ← LicenseValidator, LicenseKeyGenerator, IHardwareIdentifier
│   │
│   └── Ledger.Desktop/           ← WPF host (entry point) — net8.0-windows
│       ├── App.xaml(.cs)         ← DI bootstrap, theme, culture
│       ├── Views/                ← ActivationWindow, LoginWindow, JournalEntryView, ...
│       ├── ViewModels/           ← CommunityToolkit.Mvvm
│       └── Licensing/            ← WmiHardwareIdentifier (Windows WMI)
│
├── tests/
│   ├── Ledger.Core.Tests/        ← Money, JournalHeader, Reversal
│   └── Ledger.Services.Tests/    ← PasswordHasher, LicenseValidator
│
└── deploy/
    ├── installer/                ← Inno Setup .iss
    └── icons/                    ← app.ico, installer banners
```

**Dependency direction (strict):**

```
Ledger.Desktop  ─►  Ledger.Services  ─►  Ledger.Data  ─►  Ledger.Core  ─►  Ledger.Shared
                          │                                  ▲
                          └──────────────────────────────────┘
```

`Ledger.Core` has **zero** framework or NuGet dependencies; it's pure C#.

---

## 2. Security & licensing

### 2.1 Database encryption at rest (SQLCipher)

- The entire database file is encrypted with **AES-256 CBC + HMAC-SHA256** by **SQLCipher v4** (configured via `PRAGMA cipher_compatibility = 4`).
- We use `Microsoft.EntityFrameworkCore.Sqlite.Core` paired with `SQLitePCLRaw.bundle_e_sqlcipher` so the same EF Core API transparently encrypts/decrypts.
- The encryption key is derived from a **machine-bound passphrase** combining:
  1. The local Windows DPAPI-protected secret stored at `%LOCALAPPDATA%\Redge\db.key` (encrypted with `CurrentUser` scope).
  2. The hardware device ID.
- Without the original Windows user profile, the database file cannot be opened — even by copying the file to another PC.

### 2.2 Hardware-locked activation

The Device ID is `SHA-256( CPU.ProcessorId | BaseBoard.SerialNumber | BIOS.SerialNumber | ComputerSystemProduct.UUID )`, queried via WMI in `WmiHardwareIdentifier`.

| Property | Source | Why |
|---|---|---|
| `Win32_Processor.ProcessorId` | CPU | Survives OS reinstall |
| `Win32_BaseBoard.SerialNumber` | Motherboard | Survives disk replacement |
| `Win32_BIOS.SerialNumber` | BIOS | Survives clean install |
| `Win32_ComputerSystemProduct.UUID` | OEM | Last-resort fingerprint |

Some WMI properties are blanked on locked-down corporate machines; `WmiHardwareIdentifier` falls back to `"unknown"` for missing slots so the hash remains stable.

### 2.3 License key format

```
<base64url(payload-json)>.<base64url(rsa-2048-pkcs1-sha256-signature)>
```

The payload (`LicensePayload`):

```json
{
  "device_id":  "abc123...",
  "customer":   "Acme Inc.",
  "edition":    "Standard",
  "issued_at":  "2026-05-12T00:00:00Z",
  "expires_at": "2027-05-12",
  "reference":  "INV-001"
}
```

- **Vendor side** (`LicenseKeyGenerator`): signs with the private RSA key kept in your vault. Never ships with the app.
- **App side** (`LicenseValidator`): verifies with the embedded public PEM, then checks `device_id == GetDeviceId()` and `expires_at > today`.

Constant-time signature verification is delegated to .NET's `RSA.VerifyData` (PKCS#1 v1.5 + SHA-256).

### 2.4 Local authentication

- Passwords hashed with **BCrypt** (work factor 12). Verified via `BCrypt.Net-Next`.
- **Account lockout**: 5 failed attempts → 15 min lockout window (configurable).
- **Roles**: `Admin` (full access incl. user management, license entry, period close), `Accountant` (post entries), `DataEntry` (drafts only), `Viewer` (read-only).
- Every login / logout / role change / activation goes into the append-only `audit_log` table.

### 2.5 Threat model

| Attack | Mitigation |
|---|---|
| Stolen DB file | SQLCipher AES-256 + DPAPI machine binding |
| Pirated license key | RSA-2048 signature + per-machine device binding |
| Patching the executable to skip activation | Code signing the EXE/installer; tamper detection on `LicenseInfo` row hash (M5) |
| Brute-force password | BCrypt cost 12 + lockout after 5 attempts |
| Posted-entry tampering | Domain enforces "no edits after post"; audit log records every action |

---

## 3. Domain rules (unchanged by the offline pivot)

- All money: `decimal` in C# / `NUMERIC(18,4)` in SQLite. Zero floats.
- Exchange rates: `NUMERIC(18,8)`.
- `JournalHeader.Post(IClock, userId)` enforces `Σdebit == Σcredit` (4dp rounded) and a non-empty `Lines`; status flips Draft → Posted; PostedAt / PostedBy captured.
- Posted entries are **immutable**. `JournalHeader.CreateReversal(...)` creates an inverse entry, swaps debit/credit on each line, marks original as `Reversed`.
- Fiscal periods: `FiscalPeriod.Lock()` prevents further posting; `FiscalYear.Close()` rolls P&L into Retained Earnings (added in M3).
- Cost centers: optional per line.

---

## 4. Persistence

- One `LedgerDbContext` instance per logical operation. `UnitOfWork` wraps EF Core change tracking and transactions.
- Configurations are split per entity (one `IEntityTypeConfiguration<T>` per file under `Configurations/`).
- Initial migration is added in M3 via `dotnet ef migrations add InitialCreate --project src/Ledger.Data --startup-project src/Ledger.Desktop`.
- On first launch the app calls `Database.MigrateAsync()` after the SQLCipher key is applied.

---

## 5. UI

- **WPF + MaterialDesignInXaml 5** with custom `BundledTheme` for Light/Dark.
- **MVVM Toolkit** for `[ObservableProperty]` / `[RelayCommand]`.
- **RTL** via `FlowDirection="{Binding FlowDirection}"` on each view's root.
- **White-label**: brand colour, logo, app name are read from `appsettings.json` and `LEDGER_APP_NAME` env var.

Critical screens (M3):

1. **Activation** — shown until `LicenseInfo` row exists & is valid (`ActivationWindow.xaml` already scaffolded).
2. **Login** — username + password, BCrypt verify.
3. **Dashboard** — KPIs, LiveCharts2 revenue/expense chart.
4. **Chart of Accounts** — TreeView with parent-child rollup.
5. **Journal Entry** — DataGrid with live debit/credit totals, balance indicator, post / reverse buttons (`JournalEntryView.xaml` scaffolded).
6. **Reports** — TB, P&L, BS via QuestPDF.
7. **Settings** — users (Admin only), license info, theme, language.

---

## 6. Deployment

### Single-file EXE

```powershell
dotnet publish src\Ledger.Desktop\Ledger.Desktop.csproj `
    -c Release -r win-x64 --self-contained true
```

Project file already sets `PublishSingleFile=true`, `IncludeAllContentForSelfExtract=true`, `EnableCompressionInSingleFile=true`.

### Inno Setup installer

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" deploy\installer\installer.iss
```

The script (`deploy/installer/installer.iss`) creates `Output\Setup-Ledger-0.1.0.exe` with multi-language support (English + Arabic) and optional desktop / start-menu shortcuts.

### Code signing

Always sign the resulting Setup.exe before distribution:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a Output\Setup-Ledger-0.1.0.exe
```

---

## 7. Roadmap

| Milestone | Scope | Status |
|---|---|---|
| **M1** | Architecture + schema design | done |
| **M2** | Solution scaffold + domain + encrypted data layer + licensing + local auth (29 tests passing) | **done** |
| M3 | EF Core migrations + DI bootstrap + Activation / Login flows wired up | next |
| M4 | UI: Dashboard, COA, Journal Entry (full MVVM, RTL, theming) | |
| M5 | Reports: TB, P&L, BS via QuestPDF + Excel export | |
| M6 | White-label config + first-run wizard + sample data + license tamper detection | |
| M7 | Vendor key-issuance CLI (uses `LicenseKeyGenerator`) + code-signing pipeline | |
| M8 | QA pass + release Setup.exe | |
