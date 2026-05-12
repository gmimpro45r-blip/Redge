using Ledger.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Data.Configurations;

internal sealed class JournalHeaderConfiguration : IEntityTypeConfiguration<JournalHeader>
{
    public void Configure(EntityTypeBuilder<JournalHeader> b)
    {
        b.ToTable("journal_headers");
        b.HasKey(h => h.Id);
        b.Property(h => h.OrgId).IsRequired();
        b.Property(h => h.EntryNo).IsRequired().HasMaxLength(32);
        b.Property(h => h.Description).HasMaxLength(512);
        b.Property(h => h.Reference).HasMaxLength(128);
        b.Property(h => h.Currency).IsRequired().HasMaxLength(3);
        b.Property(h => h.ExchangeRate).HasColumnType("NUMERIC").HasPrecision(18, 8);
        b.Property(h => h.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.HasIndex(h => new { h.OrgId, h.EntryNo }).IsUnique();
        b.HasIndex(h => new { h.OrgId, h.EntryDate });
        b.HasIndex(h => h.Status);

        b.HasMany(h => h.Lines)
            .WithOne()
            .HasForeignKey(l => l.HeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Metadata.FindNavigation(nameof(JournalHeader.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> b)
    {
        b.ToTable("journal_lines");
        b.HasKey(l => l.Id);
        b.Property(l => l.LineNo).IsRequired();
        b.Property(l => l.Description).HasMaxLength(512);
        b.Property(l => l.Debit).HasColumnType("NUMERIC").HasPrecision(18, 4).IsRequired();
        b.Property(l => l.Credit).HasColumnType("NUMERIC").HasPrecision(18, 4).IsRequired();
        b.Property(l => l.DebitBase).HasColumnType("NUMERIC").HasPrecision(18, 4).IsRequired();
        b.Property(l => l.CreditBase).HasColumnType("NUMERIC").HasPrecision(18, 4).IsRequired();
        b.HasIndex(l => new { l.HeaderId, l.LineNo }).IsUnique();
        b.HasIndex(l => l.AccountId);
    }
}
