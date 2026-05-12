# Redge — Commercial Offline Accounting Desktop

> **Production-grade, 100 % offline** desktop accounting application built on **.NET 8 WPF + MaterialDesign**, with an **SQLCipher-encrypted SQLite** database, **hardware-locked licensing**, and **local BCrypt user authentication**. Designed to be **packaged into a single signed Setup.exe** and resold commercially.

---

## Highlights

- **100 % offline** — no cloud, no telemetry. All data lives in one local file.
- **SQLCipher-encrypted SQLite** — the entire database file is AES-256 encrypted on disk; nothing readable without the key.
- **Hardware-locked activation** — every install is tied to a SHA-256 fingerprint of CPU + motherboard + BIOS + product UUID via WMI.
- **RSA-2048 signed license keys** — vendor signs, app verifies with an embedded public key; payload includes customer, edition, expiry.
- **Local user management** — BCrypt-hashed passwords (work factor 12), role-based access (`Admin / Accountant / DataEntry / Viewer`), account lockout after 5 failed attempts.
- **Double-entry, immutable journal** — Σdebit = Σcredit enforced at the domain layer and at the database level; posted entries can only be corrected via a **Reversal Entry**.
- **Decimal precision everywhere** — `NUMERIC(18,4)` in the DB, `decimal` in C#. Zero floats.
- **Multi-currency** with historical exchange rates and FX gain/loss support.
- **Fiscal-year lifecycle** — period locking, year-end closing into Retained Earnings.
- **Append-only audit log** — every significant action recorded with user / entity / timestamp.
- **Reports & exports** — Trial Balance / P&L / Balance Sheet / GL via **QuestPDF** and **ClosedXML**, dashboards via **LiveCharts2**.
- **RTL (Arabic) + Dark/Light** themes via **MaterialDesignInXaml**. White-label ready.
- **Single-file self-contained EXE** packaged as a professional **Inno Setup** installer (code-signing friendly).

---

## Solution layout

```
src/
├── Ledger.Core       ← Pure domain layer (no I/O, no framework deps)
│   ├── Entities/     ← Account, JournalHeader/Line, User, LicenseInfo, AuditLog, ...
│   ├── Enums/        ← AccountType, EntryStatus, UserRole, FiscalStatus, ...
│   └── ValueObjects/ ← Money (decimal, currency-safe)
│
├── Ledger.Shared     ← Result<T>, Guard, cross-cutting helpers
│
├── Ledger.Data       ← EF Core + SQLite + SQLCipher
│   ├── LedgerDbContext.cs
│   ├── Sqlite/       ← SqlCipherConnectionFactory (PRAGMA key, WAL, ...)
│   ├── Configurations/  ← IEntityTypeConfiguration<T>
│   └── Repositories/    ← AccountRepository, JournalRepository
│
├── Ledger.Services   ← Application services
│   ├── Accounting/   ← AccountingService (draft / post / reverse)
│   ├── Auth/         ← PasswordHasher (BCrypt), LocalAuthService, UserSession
│   └── Licensing/    ← LicenseValidator, LicenseKeyGenerator, IHardwareIdentifier
│
└── Ledger.Desktop    ← WPF host (MaterialDesign, MVVM Toolkit)
    ├── Views/        ← ActivationWindow, JournalEntryView, ...
    └── Licensing/    ← WmiHardwareIdentifier (Windows-only WMI impl)

tests/
├── Ledger.Core.Tests       ← Money, JournalHeader, Reversal logic
└── Ledger.Services.Tests   ← PasswordHasher, LicenseValidator (RSA), ...

deploy/
├── installer/        ← Inno Setup .iss script
└── icons/            ← App + installer icons
```

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the full design rationale, including the licensing protocol, encryption-at-rest model, and threat analysis.

---

## Prerequisites

| Tool       | Version  | Notes |
|------------|----------|-------|
| .NET SDK   | **8.0+** | https://dotnet.microsoft.com/download |
| Windows 10+ | required | for building / running the WPF host |
| Inno Setup | 6.x      | only for building the installer (https://jrsoftware.org/isinfo.php) |

> Class libraries and tests build on Linux/macOS; only `Ledger.Desktop` requires Windows.

---

## Quick start

### 1. Clone and restore

```bash
git clone https://github.com/gmimpro45r-blip/Redge.git
cd Redge
dotnet restore
```

### 2. Run the tests (any OS)

```bash
dotnet test
```

### 3. Build & run the desktop app (Windows only)

```powershell
dotnet build
dotnet run --project src\Ledger.Desktop\Ledger.Desktop.csproj
```

On first run the app:

1. Opens the **Activation** window, shows your Device ID, and asks for an activation key.
2. After successful activation creates / opens an SQLCipher-encrypted database at `%LOCALAPPDATA%\Redge\ledger.db`.
3. Bootstraps the first **Admin** user.

---

## Licensing protocol

1. **Vendor side (offline tool):** generate a 2048-bit RSA key pair **once** with `LicenseKeyGenerator.CreateKeyPair()`. Embed the **public** PEM into the app source; keep the **private** PEM in a vendor key-vault.
2. **Customer requests activation:** they read their Device ID from the app's Activation screen and send it to you with their order.
3. **You issue a key:** sign a `LicensePayload { device_id, customer, edition, issued_at, expires_at, reference }` with the private key. The result is `{base64url-payload}.{base64url-signature}`.
4. **Customer pastes the key.** The app verifies the signature, confirms the `device_id` matches the local hardware fingerprint, and that `expires_at` (if any) is in the future.

See `src/Ledger.Services/Licensing/LicenseValidator.cs` and `LicenseKeyGenerator.cs`.

---

## Deployment

### Self-contained single-file EXE

```powershell
dotnet publish src\Ledger.Desktop\Ledger.Desktop.csproj `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true
```

Output: `src\Ledger.Desktop\bin\Release\net8.0-windows\win-x64\publish\Ledger.Desktop.exe`.

### Professional Setup.exe (Inno Setup)

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" deploy\installer\installer.iss
```

Output: `Output\Setup-Ledger-0.1.0.exe`. Sign it with your code-signing certificate:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a Output\Setup-Ledger-0.1.0.exe
```

---

## CI

GitHub Actions runs on every PR:

- `lint-libs` — restore + build + test of all class libraries on `ubuntu-latest`.
- `build-desktop` — builds `Ledger.Desktop` on `windows-latest`.

See [`.github/workflows/ci.yml`](./.github/workflows/ci.yml).

---

## Project status

This is **milestone 2 of 8** — solution scaffold, domain layer, encrypted data layer, licensing service, local auth. Tests: **29 passing (15 Core + 14 Services)**.

- [x] **M1** Architecture + schema design
- [x] **M2** Solution scaffold + domain layer + data layer (EF Core + SQLCipher) + licensing + local auth
- [ ] **M3** UI screens (Activation, Login, Dashboard, Chart of Accounts, Journal Entry) with RTL + Dark/Light
- [ ] **M4** Reports + Exports (QuestPDF, ClosedXML, LiveCharts2)
- [ ] **M5** White-label theming + first-run wizard + sample data
- [ ] **M6** Inno Setup hardening + code-signing pipeline + auto-update story
- [ ] **M7** Vendor key-issuance CLI tool
- [ ] **M8** QA pass + release

---

## License

This project is licensed under the MIT License — see [LICENSE](./LICENSE).
