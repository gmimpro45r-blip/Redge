# Professional C# Accounting System — Architecture

> **Stack**: .NET 8 WPF · MVVM Toolkit · MaterialDesignInXaml · Supabase (PostgreSQL) · SQLite (offline-first) · n8n (HMAC-SHA256 webhooks) · QuestPDF · LiveCharts2 · ClosedXML · Velopack

---

## 1. Solution Layout

```
Accounting.sln
│
├── src/
│   ├── Ledger.Core/             ← Pure domain layer  (no I/O)
│   │   ├── Entities/            ← Account, JournalHeader, JournalLine, ...
│   │   ├── Enums/               ← AccountType, EntryStatus, FiscalStatus, ...
│   │   ├── ValueObjects/        ← Money (decimal,Currency), DateRange, ...
│   │   ├── DomainEvents/        ← JournalEntryPosted, FiscalYearClosed, ...
│   │   ├── Specifications/      ← Reusable LINQ predicates
│   │   └── Abstractions/        ← Interfaces: IUnitOfWork, IClock, IUserContext
│   │
│   ├── Ledger.Data/              ← Persistence: EF Core + Supabase + SQLite
│   │   ├── LedgerDbContext.cs    ← EF Core context (used for both SQLite & PG)
│   │   ├── Configurations/       ← IEntityTypeConfiguration<T> per entity
│   │   ├── Migrations/           ← EF Core migrations (SQLite local cache)
│   │   ├── Supabase/             ← Supabase client wrapper (REST/PostgREST)
│   │   ├── Sqlite/               ← Local SQLite bootstrap + WAL
│   │   └── Repositories/        ← AccountRepository, JournalRepository, ...
│   │
│   ├── Ledger.Services/          ← Application services
│   │   ├── Accounting/           ← AccountingService (post, reverse, close)
│   │   ├── Reporting/            ← ReportService (trial balance, P&L, BS)
│   │   ├── Sync/                 ← SyncService (outbox → Supabase)
│   │   ├── Integration/          ← N8nWebhookClient (HMAC-SHA256)
│   │   ├── Auth/                 ← AuthService (Supabase JWT)
│   │   └── Export/               ← PdfExportService, ExcelExportService
│   │
│   ├── Ledger.Desktop/           ← WPF host (entry point)
│   │   ├── App.xaml(.cs)         ← DI bootstrap, theme, culture
│   │   ├── Views/                ← Window/UserControl XAML
│   │   ├── ViewModels/           ← MVVM Toolkit [ObservableProperty]
│   │   ├── Controls/             ← Reusable custom controls
│   │   ├── Resources/            ← Themes, RTL flow direction, languages
│   │   ├── Converters/           ← BoolToVisibility, DecimalFormat, ...
│   │   └── Behaviors/            ← DataGrid balance live validation
│   │
│   └── Ledger.Shared/            ← Cross-cutting: Result<T>, Logging, Guards
│
├── tests/
│   ├── Ledger.Core.Tests/        ← Unit tests (xUnit + FluentAssertions)
│   ├── Ledger.Services.Tests/    ← Service-level tests (in-memory SQLite)
│   └── Ledger.Integration.Tests/ ← Supabase + n8n contract tests
│
├── db/
│   └── schema.sql                ← Supabase schema (this repo)
│
├── deploy/
│   ├── velopack/                 ← Velopack release config
│   └── icons/                    ← App icons, installer assets
│
└── .agents/
    └── skills/                   ← Repo-specific Devin skills
```

### Why this split?

| Layer            | Depends on                | Knows about     |
|------------------|---------------------------|-----------------|
| `Ledger.Core`    | nothing                   | domain only     |
| `Ledger.Data`    | `Core`                    | EF Core, Supabase REST, SQLite |
| `Ledger.Services`| `Core`, `Data`            | use cases       |
| `Ledger.Desktop` | `Services`, `Core`        | WPF, MaterialDesign |
| `Ledger.Shared`  | nothing                   | utilities       |

Core has **zero** dependencies on frameworks — this is what makes the business logic testable and portable (could be re-hosted as a Blazor or Web API later).

---

## 2. Key NuGet Packages

| Concern          | Package(s) |
|------------------|------------|
| MVVM             | `CommunityToolkit.Mvvm` |
| UI               | `MaterialDesignThemes`, `MaterialDesignColors` |
| DI               | `Microsoft.Extensions.DependencyInjection`, `.Hosting` |
| EF Core (SQLite) | `Microsoft.EntityFrameworkCore.Sqlite`, `.Design` |
| Supabase client  | `supabase-csharp` (Postgrest + Realtime + Storage + Auth) |
| Validation       | `FluentValidation` |
| PDF              | `QuestPDF` |
| Excel            | `ClosedXML` |
| Charts           | `LiveChartsCore.SkiaSharpView.WPF` |
| Logging          | `Serilog`, `Serilog.Sinks.File`, `Serilog.Sinks.Debug` |
| HTTP             | `Microsoft.Extensions.Http.Polly` (retry + circuit breaker) |
| Hashing/HMAC     | `System.Security.Cryptography` (built-in) |
| Packaging        | `Velopack` |

---

## 3. Domain Model (Core)

```csharp
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string c) => new(0m, c);
    public Money Add(Money o)
    {
        if (Currency != o.Currency) throw new InvalidOperationException("currency mismatch");
        return this with { Amount = decimal.Round(Amount + o.Amount, 4) };
    }
    public Money Multiply(decimal rate)
        => this with { Amount = decimal.Round(Amount * rate, 4) };
}

public sealed class JournalHeader : Entity
{
    public Guid    OrgId         { get; private set; }
    public string  EntryNo       { get; private set; } = default!;
    public DateOnly EntryDate    { get; private set; }
    public string? Description   { get; private set; }
    public string  Currency      { get; private set; } = "USD";
    public decimal ExchangeRate  { get; private set; } = 1m;
    public Guid?   FiscalPeriodId{ get; private set; }
    public EntryStatus Status    { get; private set; } = EntryStatus.Draft;
    public Guid?   ReversesId    { get; private set; }
    public Guid?   ReversedById  { get; private set; }
    public IReadOnlyList<JournalLine> Lines => _lines;

    private readonly List<JournalLine> _lines = new();

    public void AddLine(JournalLine line) { /* invariants */ }

    public Result Post(IClock clock, Guid userId)
    {
        if (_lines.Count == 0) return Result.Fail("EMPTY_ENTRY");
        var debit  = _lines.Sum(l => l.Debit);
        var credit = _lines.Sum(l => l.Credit);
        if (decimal.Round(debit,4) != decimal.Round(credit,4))
            return Result.Fail($"UNBALANCED: debit={debit} credit={credit}");
        Status   = EntryStatus.Posted;
        PostedAt = clock.UtcNow;
        PostedBy = userId;
        return Result.Ok();
    }

    public JournalHeader CreateReversal(IClock clock, Guid userId)
    {
        if (Status != EntryStatus.Posted)
            throw new InvalidOperationException("only posted entries can be reversed");
        var reversal = new JournalHeader { /* swap debit/credit */ };
        ReversedById = reversal.Id;
        Status       = EntryStatus.Reversed;
        return reversal;
    }
}
```

Decimal precision: all monetary fields use `decimal` (mapped to `numeric(18,4)`). **No `float` / `double` anywhere in the domain.**

---

## 4. Service Layer

### `AccountingService`
- `Task<Result<Guid>> CreateDraftAsync(JournalEntryDto)`
- `Task<Result> PostAsync(Guid headerId)`
- `Task<Result<Guid>> ReverseAsync(Guid headerId, string reason)`
- `Task<Result<Guid>> CloseFiscalYearAsync(Guid fiscalYearId, Guid retainedEarningsAccountId)` → calls Supabase RPC `close_fiscal_year`.

### `SyncService` (offline-first)
1. On every write the repository inserts a row into local **`sync_outbox`**.
2. A `BackgroundService` polls connectivity (HEAD on `${N8N_DOMAIN}/healthz` + Supabase ping).
3. When online: drain outbox → Supabase via PostgREST (idempotent upserts keyed on `id`).
4. Conflict resolution: server-wins for `currencies`, client-wins for `journal_*` drafts, hard-fail (manual) for posted entries.

### `IntegrationService` — n8n webhooks (HMAC-SHA256)

```csharp
public sealed class N8nWebhookClient
{
    private readonly HttpClient _http;
    private readonly string _secret;     // injected from config

    public async Task SendAsync(string path, object payload, CancellationToken ct)
    {
        var json      = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign($"{timestamp}.{json}", _secret);

        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Devin-Timestamp", timestamp);
        req.Headers.Add("X-Devin-Signature", $"sha256={signature}");

        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    private static string Sign(string data, string secret)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
}
```

On the n8n side, the workflow's **Webhook node** verifies the signature in a Code node before any downstream action runs. Replay protection: reject if `|now - timestamp| > 300s`.

### `AuthService` (Supabase JWT)
- Wraps `Supabase.Gotrue.Client`.
- Stores refresh token encrypted with **DPAPI** (`ProtectedData.Protect`) at `%LOCALAPPDATA%\Ledger\auth.bin`.
- On startup: silently refresh; if it fails, show login window.
- All outgoing PostgREST requests include `Authorization: Bearer <jwt>` so RLS policies apply.

---

## 5. UI / UX

- **MaterialDesignInXaml** with `BundledTheme` for Dark/Light toggle.
- **RTL support**: `FlowDirection="RightToLeft"` driven from `CultureService.Current.IsRightToLeft`.
- Localisation via `.resx` files (`Strings.en.resx`, `Strings.ar.resx`).
- All numbers formatted with `CultureInfo.GetCultureInfo("ar-EG")` for Arabic UI (Eastern Arabic digits opt-in).
- DataGrid for journal lines uses a `LiveBalanceBehavior` that recomputes Σdebit/Σcredit on every CellEditEnding and disables the **Post** button until balanced.

### Screens
1. **Login** — Supabase email/password + "Remember me".
2. **Dashboard** — KPI tiles + LiveCharts2 (Revenue vs Expense, Cash flow, Top accounts).
3. **Chart of Accounts** — `TreeView` bound to recursive accounts query.
4. **Journal Entry** — header card + lines `DataGrid` + balance footer; **Save Draft / Post / Reverse**.
5. **Trial Balance / P&L / Balance Sheet** — DataGrid + Export buttons.
6. **Fiscal Year Manager** — list of years, lock/close actions.
7. **Settings** — currencies, exchange rates, cost centers, n8n endpoint + secret.

---

## 6. Configuration & Secrets

`appsettings.json` ships with placeholders only:

```json
{
  "Supabase": {
    "Url":       "https://YOUR-PROJECT.supabase.co",
    "AnonKey":   "${SUPABASE_ANON_KEY}"
  },
  "N8n": {
    "Domain":    "${N8N_DOMAIN}",
    "ApiKey":    "${N8N_API_KEY}",
    "WebhookSecret": "${N8N_WEBHOOK_SECRET}"
  }
}
```

At runtime values are sourced from (in order):
1. `appsettings.{Environment}.json`
2. Environment variables (`Ledger__Supabase__Url`, etc.)
3. **DPAPI-encrypted user secrets** at `%LOCALAPPDATA%\Ledger\secrets.bin`.

The user never types secrets into XAML — the Settings screen writes through `ISecretStore`.

---

## 7. Sync & Offline-First Strategy

```
┌───────────────────┐   write   ┌──────────────┐
│  ViewModel        │──────────▶│  Repository  │
└───────────────────┘           └──────┬───────┘
                                       │ same TX
                                       ▼
                              ┌──────────────────┐
                              │ SQLite (local)   │
                              │   + sync_outbox  │
                              └──────┬───────────┘
                                     │ BackgroundService
                                     ▼
                              ┌──────────────────┐
                              │ Supabase (cloud) │
                              └──────────────────┘
```

- Every write is local-first → app stays responsive even without internet.
- Outbox is the source of truth for "what needs to go to the cloud".
- Posted journal entries can be modified only via a **reversal entry** — the same constraint exists in the DB (triggers) and the domain (`JournalHeader.Post`).

---

## 8. Reporting

| Report          | Source                          | Renderer  | Export |
|-----------------|---------------------------------|-----------|--------|
| Trial Balance   | `v_trial_balance`               | DataGrid  | QuestPDF, ClosedXML |
| Profit & Loss   | `v_trial_balance` filtered      | DataGrid  | QuestPDF, ClosedXML |
| Balance Sheet   | `v_account_balances_rollup`     | DataGrid  | QuestPDF, ClosedXML |
| Cash Flow       | `journal_lines` ∩ cash accounts | DataGrid  | QuestPDF |
| GL by Account   | `journal_lines` by account      | DataGrid  | QuestPDF, ClosedXML |
| Dashboards      | aggregated views                | LiveCharts2 | PNG snapshot |

QuestPDF documents live under `Ledger.Services/Export/Pdf/Documents/*.cs` — one class per report, each implementing `IDocument`.

---

## 9. Security Checklist

- [x] All financial values stored as `numeric(18,4)` / `decimal`.
- [x] Hard deletes blocked by trigger on `journal_headers`.
- [x] Σdebit = Σcredit enforced server-side **and** client-side.
- [x] Period lock enforced server-side.
- [x] Supabase RLS: every business table is restricted by `is_org_member(org_id)`.
- [x] JWT refresh token encrypted with DPAPI on disk.
- [x] n8n webhooks signed with HMAC-SHA256 + 5-minute replay window.
- [x] Audit log is append-only (no UPDATE/DELETE policies).
- [x] No secrets in source control — placeholders only in `appsettings.json`.

---

## 10. Deployment (Velopack — Single-File EXE + auto-update)

```bash
# 1. Publish self-contained, single-file, trimmed
dotnet publish src/Ledger.Desktop/Ledger.Desktop.csproj \
    -c Release -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true

# 2. Pack as a Velopack release
dotnet tool install --global vpk

vpk pack \
    --packId      Ledger.Desktop \
    --packVersion 1.0.0 \
    --packDir     src/Ledger.Desktop/bin/Release/net8.0-windows/win-x64/publish \
    --mainExe     Ledger.Desktop.exe \
    --icon        deploy/icons/app.ico

# 3. Upload the produced ./Releases/* to your update channel
#    (S3, GitHub Releases, Supabase Storage, ...)
```

Auto-update on launch:

```csharp
var mgr = new UpdateManager("https://updates.example.com/ledger");
var info = await mgr.CheckForUpdatesAsync();
if (info != null) {
    await mgr.DownloadUpdatesAsync(info);
    mgr.ApplyUpdatesAndRestart();
}
```

---

## 11. Milestones (proposed)

| # | Milestone                                 | Deliverable                                |
|---|--------------------------------------------|--------------------------------------------|
| 1 | Schema + Architecture                      | **this file** + `schema.sql`               |
| 2 | Solution scaffold + Core models            | empty WPF window + entities + unit tests   |
| 3 | Data layer + Supabase + SQLite             | EF Core context, repos, migrations         |
| 4 | Services + Sync engine                     | AccountingService, SyncService             |
| 5 | UI screens (Login, COA, Journal)           | usable end-to-end happy path               |
| 6 | Reports + Exports                          | Trial Balance / P&L / BS + PDF / Excel     |
| 7 | n8n integration + signed webhooks          | working webhook → n8n workflow             |
| 8 | Velopack packaging + auto-update           | single-file EXE                            |

I'll wait for your go-ahead on solution name + repo location before scaffolding step 2.
