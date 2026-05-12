# Redge — Professional WPF Accounting System

> Production-grade Desktop Accounting app on **.NET 8 WPF + MaterialDesign**, backed by **Supabase (PostgreSQL)** with **SQLite offline cache** and **n8n** webhook automation.

---

## Highlights

- **Double-entry, immutable journal** — Σdebit = Σcredit enforced both client-side and via PostgreSQL triggers. Posted entries can only be corrected via a **Reversal Entry**.
- **Decimal precision everywhere** — `numeric(18,4)` in the DB, `decimal` in C#. Zero floats.
- **Multi-currency** with historical exchange rates and automated FX gain/loss.
- **Multi-tenant** with Supabase **Row-Level Security** keyed on `org_id` and user role.
- **Offline-first** — every write hits SQLite + a `sync_outbox`; a background service drains it to Supabase when online.
- **Fiscal-year lifecycle** — period locking, year-end closing into Retained Earnings.
- **n8n automation** — outgoing webhooks signed with **HMAC-SHA256** (+ 5-minute replay window).
- **Reports & exports** — Trial Balance / P&L / Balance Sheet / GL via **QuestPDF** and **ClosedXML**, dashboards via **LiveCharts2**.
- **RTL + Dark/Light** via **MaterialDesignInXaml**.
- **Velopack** single-file EXE + auto-update.

---

## Solution layout

```
src/
├── Ledger.Core       ← Pure domain layer (no I/O, no framework deps)
├── Ledger.Shared     ← Result<T>, guards, cross-cutting helpers
├── Ledger.Data       ← EF Core + Supabase + SQLite, repositories
├── Ledger.Services   ← AccountingService, SyncService, IntegrationService
└── Ledger.Desktop    ← WPF host (MaterialDesign, MVVM Toolkit)

tests/
├── Ledger.Core.Tests
└── Ledger.Services.Tests

db/
└── schema.sql        ← Supabase PostgreSQL schema

deploy/
├── velopack/         ← Velopack release config
└── icons/            ← Installer assets
```

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the full design rationale.

---

## Prerequisites

| Tool          | Version  | Notes |
|---------------|----------|-------|
| .NET SDK      | **8.0+** | https://dotnet.microsoft.com/download |
| Windows 10+   | required | for building / running the WPF host |
| PostgreSQL CLI (`psql`) | 15+ | only for applying the schema |
| Supabase account | free tier OK | https://supabase.com |
| n8n           | Cloud or self-hosted | optional, for webhook automation |

> Class libraries and tests build on Linux/macOS; only `Ledger.Desktop` requires Windows.

---

## Quick start

### 1. Clone and restore

```bash
git clone https://github.com/gmimpro45-blip/Redge.git
cd Redge
dotnet restore
```

### 2. Create a Supabase project & apply the schema

1. Create a new project at https://supabase.com (free tier is fine).
2. Copy the **Database Connection String** from *Settings → Database → Connection string* (URI mode).
3. Apply the schema:

```bash
# Linux / macOS / WSL
./scripts/setup-supabase.sh "postgresql://postgres:<password>@db.<ref>.supabase.co:5432/postgres"

# Windows PowerShell
.\scripts\setup-supabase.ps1 -ConnectionString "postgresql://postgres:<password>@db.<ref>.supabase.co:5432/postgres"
```

### 3. Configure secrets

Copy the example env file and fill in the values:

```bash
cp .env.example .env
```

| Variable          | Description |
|-------------------|-------------|
| `SUPABASE_URL`    | `https://<ref>.supabase.co` |
| `SUPABASE_ANON_KEY` | from *Settings → API* |
| `N8N_DOMAIN`      | your n8n base URL (no trailing slash) |
| `N8N_API_KEY`     | n8n API key |
| `N8N_WEBHOOK_SECRET` | shared secret used to sign HMAC-SHA256 webhook requests |

The desktop app reads them via `Microsoft.Extensions.Configuration` → environment variables → `appsettings.json`.

### 4. Build and run (Windows only for the desktop app)

```powershell
dotnet build
dotnet run --project src/Ledger.Desktop/Ledger.Desktop.csproj
```

Tests work on any OS:

```bash
dotnet test
```

---

## CI

GitHub Actions runs on every PR:

- `lint-libs` — restore + build + test of all class libraries on `ubuntu-latest`.
- `build-desktop` — builds `Ledger.Desktop` on `windows-latest`.

See [`.github/workflows/ci.yml`](./.github/workflows/ci.yml).

---

## Deployment (Velopack single-file EXE)

```powershell
dotnet publish src/Ledger.Desktop/Ledger.Desktop.csproj `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true

dotnet tool install --global vpk
vpk pack `
    --packId Ledger.Desktop `
    --packVersion 0.1.0 `
    --packDir src/Ledger.Desktop/bin/Release/net8.0-windows/win-x64/publish `
    --mainExe Ledger.Desktop.exe
```

---

## Project status

This is **milestone 2 of 8** — solution scaffold, domain layer, and CI in place. Subsequent PRs:

- [ ] **M3** Data layer (EF Core context, repositories, Supabase client)
- [ ] **M4** Services + Sync engine + Auth
- [ ] **M5** UI screens (Login, Dashboard, Chart of Accounts, Journal Entry)
- [ ] **M6** Reports + Exports (QuestPDF, ClosedXML, LiveCharts2)
- [ ] **M7** n8n integration (signed webhooks)
- [ ] **M8** Velopack packaging + auto-update

---

## License

[MIT](./LICENSE)
