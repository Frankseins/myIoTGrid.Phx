using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("Readings");

        // Reading uses long Id for performance
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.NodeId)
            .IsRequired();

        // AssignmentId is optional (for direct sensor readings)
        builder.Property(r => r.AssignmentId);

        builder.Property(r => r.MeasurementType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.RawValue)
            .IsRequired();

        builder.Property(r => r.Value)
            .IsRequired();

        builder.Property(r => r.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Timestamp)
            .IsRequired();

        builder.Property(r => r.IsSyncedToCloud)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(r => r.Node)
            .WithMany(n => n.Readings)
            .HasForeignKey(r => r.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Assignment)
            .WithMany(a => a.Readings)
            .HasForeignKey(r => r.AssignmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for efficient querying
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.NodeId);
        builder.HasIndex(r => r.AssignmentId);
        builder.HasIndex(r => r.Timestamp);
        builder.HasIndex(r => r.MeasurementType);
        builder.HasIndex(r => r.IsSyncedToCloud);

        // Composite indexes for common queries
        builder.HasIndex(r => new { r.NodeId, r.Timestamp });
        builder.HasIndex(r => new { r.NodeId, r.MeasurementType, r.Timestamp });
        builder.HasIndex(r => new { r.TenantId, r.Timestamp });
    }
}
