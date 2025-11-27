using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Sensor Entity (ESP32/LoRa32 Device)
/// </summary>
public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");

        // Primary Key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.HubId)
            .IsRequired();

        builder.Property(s => s.SensorId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Protocol)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(s => s.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Location as Owned Entity
        builder.OwnsOne(s => s.Location, location =>
        {
            location.Property(l => l.Name)
                .HasColumnName("Location_Name")
                .HasMaxLength(200);

            location.Property(l => l.Latitude)
                .HasColumnName("Location_Latitude");

            location.Property(l => l.Longitude)
                .HasColumnName("Location_Longitude");
        });

        // SensorTypes as CSV (comma-separated values)
        builder.Property(s => s.SensorTypes)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .HasMaxLength(500)
            .HasColumnName("SensorTypes");

        // Relationships
        builder.HasOne(s => s.Hub)
            .WithMany(h => h.Sensors)
            .HasForeignKey(s => s.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.SensorData)
            .WithOne(sd => sd.Sensor)
            .HasForeignKey(sd => sd.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.HubId);
        builder.HasIndex(s => s.SensorId);
        builder.HasIndex(s => new { s.HubId, s.SensorId }).IsUnique();
        builder.HasIndex(s => s.IsOnline);
        builder.HasIndex(s => s.LastSeen);
    }
}
