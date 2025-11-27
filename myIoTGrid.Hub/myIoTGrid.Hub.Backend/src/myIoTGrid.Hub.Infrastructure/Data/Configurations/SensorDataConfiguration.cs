using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for SensorData Entity
/// </summary>
public class SensorDataConfiguration : IEntityTypeConfiguration<SensorData>
{
    public void Configure(EntityTypeBuilder<SensorData> builder)
    {
        builder.ToTable("SensorData");

        // Primary Key
        builder.HasKey(sd => sd.Id);

        // Properties
        builder.Property(sd => sd.TenantId)
            .IsRequired();

        builder.Property(sd => sd.SensorId)
            .IsRequired();

        builder.Property(sd => sd.SensorTypeId)
            .IsRequired();

        builder.Property(sd => sd.Value)
            .IsRequired();

        builder.Property(sd => sd.Timestamp)
            .IsRequired();

        builder.Property(sd => sd.IsSyncedToCloud)
            .IsRequired()
            .HasDefaultValue(false);

        // Location is NOT stored in SensorData anymore - it comes from Sensor
        // builder.OwnsOne(sd => sd.Location, ...);  // REMOVED!

        // Relationships
        builder.HasOne(sd => sd.Sensor)
            .WithMany(s => s.SensorData)
            .HasForeignKey(sd => sd.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.SensorType)
            .WithMany(st => st.SensorData)
            .HasForeignKey(sd => sd.SensorTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for Performance (time-series queries)
        builder.HasIndex(sd => sd.TenantId);
        builder.HasIndex(sd => sd.SensorId);
        builder.HasIndex(sd => sd.SensorTypeId);
        builder.HasIndex(sd => sd.Timestamp);
        builder.HasIndex(sd => sd.IsSyncedToCloud);
        builder.HasIndex(sd => new { sd.TenantId, sd.Timestamp });
        builder.HasIndex(sd => new { sd.SensorId, sd.Timestamp });
        builder.HasIndex(sd => new { sd.SensorId, sd.SensorTypeId, sd.Timestamp });
    }
}
