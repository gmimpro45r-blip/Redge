using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrgId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    NormalBalance = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsPostable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrgId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_centers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Decimals = table.Column<short>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrgId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ToCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 8, nullable: false),
                    RateDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_years",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrgId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    RetainedEarningsAccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_years", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "journal_headers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrgId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryNo = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 8, nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ReversedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReversesId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PostedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_headers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ActivationKey = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FailedLoginCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HeaderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineNo = table.Column<short>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Debit = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 4, nullable: false),
                    DebitBase = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 4, nullable: false),
                    CreditBase = table.Column<decimal>(type: "NUMERIC", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_lines_journal_headers_HeaderId",
                        column: x => x.HeaderId,
                        principalTable: "journal_headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_OrgId_Code",
                table: "accounts",
                columns: new[] { "OrgId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_ParentId",
                table: "accounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_At",
                table: "audit_log",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_EntityName_EntityId",
                table: "audit_log",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_OrgId_Code",
                table: "cost_centers",
                columns: new[] { "OrgId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_Code",
                table: "currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_OrgId_FromCurrency_ToCurrency_RateDate",
                table: "exchange_rates",
                columns: new[] { "OrgId", "FromCurrency", "ToCurrency", "RateDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_periods_FiscalYearId_Code",
                table: "fiscal_periods",
                columns: new[] { "FiscalYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_years_OrgId_Code",
                table: "fiscal_years",
                columns: new[] { "OrgId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_headers_OrgId_EntryDate",
                table: "journal_headers",
                columns: new[] { "OrgId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_journal_headers_OrgId_EntryNo",
                table: "journal_headers",
                columns: new[] { "OrgId", "EntryNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_headers_Status",
                table: "journal_headers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_AccountId",
                table: "journal_lines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_HeaderId_LineNo",
                table: "journal_lines",
                columns: new[] { "HeaderId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licenses_ActivatedAt",
                table: "licenses",
                column: "ActivatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "cost_centers");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "fiscal_periods");

            migrationBuilder.DropTable(
                name: "fiscal_years");

            migrationBuilder.DropTable(
                name: "journal_lines");

            migrationBuilder.DropTable(
                name: "licenses");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "journal_headers");
        }
    }
}
