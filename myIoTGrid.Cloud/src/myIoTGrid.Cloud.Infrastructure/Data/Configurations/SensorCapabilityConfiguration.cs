using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class SensorCapabilityConfiguration : IEntityTypeConfiguration<SensorCapability>
{
    public void Configure(EntityTypeBuilder<SensorCapability> builder)
    {
        builder.ToTable("SensorCapabilities");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.SensorId)
            .IsRequired();

        builder.Property(sc => sc.MeasurementType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sc => sc.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sc => sc.Resolution)
            .IsRequired()
            .HasDefaultValue(0.01);

        builder.Property(sc => sc.Accuracy)
            .IsRequired()
            .HasDefaultValue(0.5);

        builder.Property(sc => sc.MatterClusterName)
            .HasMaxLength(100);

        builder.Property(sc => sc.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(sc => sc.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(sc => sc.Sensor)
            .WithMany(s => s.Capabilities)
            .HasForeignKey(sc => sc.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sc => sc.SensorId);
        builder.HasIndex(sc => new { sc.SensorId, sc.MeasurementType }).IsUnique();
    }
}
