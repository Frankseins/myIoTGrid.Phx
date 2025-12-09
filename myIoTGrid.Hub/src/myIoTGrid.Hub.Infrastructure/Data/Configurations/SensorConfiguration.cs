using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Sensor Entity.
/// Complete sensor definition with hardware configuration and calibration (v3.0).
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");

        // Primary Key
        builder.HasKey(s => s.Id);

        // === Identification ===

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.SerialNumber)
            .HasMaxLength(100);

        // === Hardware Info ===

        builder.Property(s => s.Manufacturer)
            .HasMaxLength(100);

        builder.Property(s => s.Model)
            .HasMaxLength(100);

        builder.Property(s => s.DatasheetUrl)
            .HasMaxLength(500);

        // === Communication Protocol ===

        builder.Property(s => s.Protocol)
            .IsRequired();

        // === Pin Configuration ===

        builder.Property(s => s.I2CAddress)
            .HasMaxLength(10);

        // === Timing Configuration ===

        builder.Property(s => s.IntervalSeconds)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(s => s.MinIntervalSeconds)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.WarmupTimeMs)
            .HasDefaultValue(0);

        // === Calibration ===

        builder.Property(s => s.OffsetCorrection)
            .HasDefaultValue(0.0);

        builder.Property(s => s.GainCorrection)
            .HasDefaultValue(1.0);

        builder.Property(s => s.CalibrationNotes)
            .HasMaxLength(1000);

        // === Categorization ===

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Icon)
            .HasMaxLength(50);

        builder.Property(s => s.Color)
            .HasMaxLength(20);

        // === Status ===

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // === Relationships ===

        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Capabilities)
            .WithOne(c => c.Sensor)
            .HasForeignKey(c => c.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.NodeAssignments)
            .WithOne(a => a.Sensor)
            .HasForeignKey(a => a.SensorId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Indexes ===

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.Code);
        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
        builder.HasIndex(s => s.Category);
        builder.HasIndex(s => s.Protocol);
        builder.HasIndex(s => s.SerialNumber);
        builder.HasIndex(s => s.IsActive);
    }
}
