using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for SensorCapability Entity.
/// Defines measurement capabilities of a Sensor (v3.0).
/// Matter-konform: Corresponds to a Matter Cluster.
/// </summary>
public class SensorCapabilityConfiguration : IEntityTypeConfiguration<SensorCapability>
{
    public void Configure(EntityTypeBuilder<SensorCapability> builder)
    {
        builder.ToTable("SensorCapabilities");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.SensorId)
            .IsRequired();

        builder.Property(c => c.MeasurementType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Resolution)
            .HasDefaultValue(0.01);

        builder.Property(c => c.Accuracy)
            .HasDefaultValue(0.5);

        builder.Property(c => c.MatterClusterName)
            .HasMaxLength(100);

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(c => c.SensorId);
        builder.HasIndex(c => c.MeasurementType);
        builder.HasIndex(c => new { c.SensorId, c.MeasurementType }).IsUnique();
        builder.HasIndex(c => c.MatterClusterId);
    }
}
