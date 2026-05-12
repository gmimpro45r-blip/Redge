using Ledger.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Data.Configurations;

internal sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> b)
    {
        b.ToTable("cost_centers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Code).IsRequired().HasMaxLength(64);
        b.Property(c => c.Name).IsRequired().HasMaxLength(256);
        b.HasIndex(c => new { c.OrgId, c.Code }).IsUnique();
    }
}

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> b)
    {
        b.ToTable("currencies");
        b.HasKey(c => c.Id);
        b.Property(c => c.Code).IsRequired().HasMaxLength(3);
        b.Property(c => c.Name).IsRequired().HasMaxLength(64);
        b.Property(c => c.Symbol).HasMaxLength(8);
        b.HasIndex(c => c.Code).IsUnique();
    }
}

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> b)
    {
        b.ToTable("exchange_rates");
        b.HasKey(r => r.Id);
        b.Property(r => r.FromCurrency).IsRequired().HasMaxLength(3);
        b.Property(r => r.ToCurrency).IsRequired().HasMaxLength(3);
        b.Property(r => r.Rate).HasColumnType("NUMERIC").HasPrecision(18, 8);
        b.HasIndex(r => new { r.OrgId, r.FromCurrency, r.ToCurrency, r.RateDate }).IsUnique();
    }
}

internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> b)
    {
        b.ToTable("fiscal_years");
        b.HasKey(f => f.Id);
        b.Property(f => f.Code).IsRequired().HasMaxLength(32);
        b.Property(f => f.Status).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(f => new { f.OrgId, f.Code }).IsUnique();
    }
}

internal sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> b)
    {
        b.ToTable("fiscal_periods");
        b.HasKey(p => p.Id);
        b.Property(p => p.Code).IsRequired().HasMaxLength(32);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(p => new { p.FiscalYearId, p.Code }).IsUnique();
    }
}
