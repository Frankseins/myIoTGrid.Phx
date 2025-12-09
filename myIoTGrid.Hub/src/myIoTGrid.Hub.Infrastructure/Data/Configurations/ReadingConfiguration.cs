using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Reading Entity.
/// Time-series measurement with raw and calibrated values.
/// </summary>
public class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("Readings");

        // Primary Key - using long Id for performance (auto-increment)
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.NodeId)
            .IsRequired();

        // AssignmentId is optional - direct sensor readings don't have an assignment
        builder.Property(r => r.AssignmentId)
            .IsRequired(false);

        builder.Property(r => r.MeasurementType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.RawValue)
            .IsRequired();

        builder.Property(r => r.Value)
            .IsRequired();

        builder.Property(r => r.Unit)
            .IsRequired()
            .HasMaxLength(20);

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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for Performance (time-series queries)
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.NodeId);
        builder.HasIndex(r => r.AssignmentId);
        builder.HasIndex(r => r.MeasurementType);
        builder.HasIndex(r => r.Timestamp);
        builder.HasIndex(r => r.IsSyncedToCloud);
        builder.HasIndex(r => new { r.TenantId, r.Timestamp });
        builder.HasIndex(r => new { r.NodeId, r.Timestamp });
        builder.HasIndex(r => new { r.AssignmentId, r.Timestamp });
        builder.HasIndex(r => new { r.AssignmentId, r.MeasurementType, r.Timestamp });
    }
}
