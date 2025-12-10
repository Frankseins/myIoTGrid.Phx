using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.HubId);

        builder.Property(a => a.NodeId);

        builder.Property(a => a.AlertTypeId)
            .IsRequired();

        builder.Property(a => a.Level)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.Recommendation)
            .HasMaxLength(2000);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.ExpiresAt);

        builder.Property(a => a.AcknowledgedAt);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

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
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.CreatedAt);

        // Composite index for active alerts query
        builder.HasIndex(a => new { a.TenantId, a.IsActive, a.CreatedAt });
    }
}
