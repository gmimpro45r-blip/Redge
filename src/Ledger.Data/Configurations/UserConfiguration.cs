using Ledger.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Username).IsRequired().HasMaxLength(64);
        b.Property(u => u.DisplayName).IsRequired().HasMaxLength(128);
        b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.HasIndex(u => u.Username).IsUnique();
    }
}

internal sealed class LicenseInfoConfiguration : IEntityTypeConfiguration<LicenseInfo>
{
    public void Configure(EntityTypeBuilder<LicenseInfo> b)
    {
        b.ToTable("licenses");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceId).IsRequired().HasMaxLength(128);
        b.Property(x => x.ActivationKey).IsRequired().HasMaxLength(4096);
        b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
        b.Property(x => x.Edition).IsRequired().HasMaxLength(32);
        b.HasIndex(x => x.ActivatedAt);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(64);
        b.Property(x => x.EntityName).HasMaxLength(64);
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.HasIndex(x => x.At);
        b.HasIndex(x => new { x.EntityName, x.EntityId });
    }
}
