using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Hub Entity (Raspberry Pi Gateway)
/// </summary>
public class HubConfiguration : IEntityTypeConfiguration<myIoTGrid.Shared.Common.Entities.Hub>
{
    public void Configure(EntityTypeBuilder<myIoTGrid.Shared.Common.Entities.Hub> builder)
    {
        builder.ToTable("Hubs");

        // Primary Key
        builder.HasKey(h => h.Id);

        // Properties
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

        // Relationships
        builder.HasOne(h => h.Tenant)
            .WithMany(t => t.Hubs)
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Nodes)
            .WithOne(n => n.Hub)
            .HasForeignKey(n => n.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Alerts)
            .WithOne(a => a.Hub)
            .HasForeignKey(a => a.HubId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        // Single-Hub-Architecture: Only ONE Hub per Tenant allowed
        builder.HasIndex(h => h.TenantId)
            .IsUnique()
            .HasDatabaseName("IX_Hub_TenantId_Unique");

        builder.HasIndex(h => h.HubId);
        builder.HasIndex(h => h.IsOnline);
        builder.HasIndex(h => h.LastSeen);
    }
}
