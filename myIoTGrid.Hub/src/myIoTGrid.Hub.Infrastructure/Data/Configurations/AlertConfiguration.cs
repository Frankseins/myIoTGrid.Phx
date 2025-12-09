using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Alert Entity
/// </summary>
public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.AlertTypeId)
            .IsRequired();

        builder.Property(a => a.Level)
            .IsRequired();

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Recommendation)
            .HasMaxLength(1000);

        builder.Property(a => a.Source)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(a => a.Tenant)
            .WithMany(t => t.Alerts)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Hub)
            .WithMany(h => h.Alerts)
            .HasForeignKey(a => a.HubId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Node)
            .WithMany(n => n.Alerts)
            .HasForeignKey(a => a.NodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.AlertType)
            .WithMany(at => at.Alerts)
            .HasForeignKey(a => a.AlertTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.HubId);
        builder.HasIndex(a => a.NodeId);
        builder.HasIndex(a => a.AlertTypeId);
        builder.HasIndex(a => a.Level);
        builder.HasIndex(a => a.Source);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.TenantId, a.IsActive });
        builder.HasIndex(a => new { a.TenantId, a.Level, a.IsActive });
    }
}
