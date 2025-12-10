using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class HubConfiguration : IEntityTypeConfiguration<Hub>
{
    public void Configure(EntityTypeBuilder<Hub> builder)
    {
        builder.ToTable("Hubs");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.TenantId)
            .IsRequired();

        builder.Property(h => h.HubId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Description)
            .HasMaxLength(500);

        builder.Property(h => h.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        // Default Provisioning Settings
        builder.Property(h => h.DefaultWifiSsid)
            .HasMaxLength(64);

        builder.Property(h => h.DefaultWifiPassword)
            .HasMaxLength(256);

        builder.Property(h => h.ApiUrl)
            .HasMaxLength(256);

        builder.Property(h => h.ApiPort)
            .IsRequired()
            .HasDefaultValue(5002);

        // Relationships
        builder.HasOne(h => h.Tenant)
            .WithMany(t => t.Hubs)
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(h => h.TenantId);
        builder.HasIndex(h => new { h.TenantId, h.HubId }).IsUnique();
        builder.HasIndex(h => h.IsOnline);
    }
}
