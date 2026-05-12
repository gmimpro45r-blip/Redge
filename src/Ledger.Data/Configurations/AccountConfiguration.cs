using Ledger.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Data.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");
        b.HasKey(a => a.Id);
        b.Property(a => a.OrgId).IsRequired();
        b.Property(a => a.Code).IsRequired().HasMaxLength(64);
        b.Property(a => a.Name).IsRequired().HasMaxLength(256);
        b.Property(a => a.NameAr).HasMaxLength(256);
        b.Property(a => a.AccountType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(a => a.NormalBalance).HasConversion<string>().HasMaxLength(8).IsRequired();
        b.Property(a => a.Currency).IsRequired().HasMaxLength(3);
        b.HasIndex(a => new { a.OrgId, a.Code }).IsUnique();
        b.HasIndex(a => a.ParentId);
    }
}
