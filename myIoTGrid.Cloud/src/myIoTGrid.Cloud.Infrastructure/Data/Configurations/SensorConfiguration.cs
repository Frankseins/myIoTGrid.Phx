using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId)
            .IsRequired();

        // Identification
        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.SerialNumber)
            .HasMaxLength(100);

        // Hardware Info
        builder.Property(s => s.Manufacturer)
            .HasMaxLength(100);

        builder.Property(s => s.Model)
            .HasMaxLength(100);

        builder.Property(s => s.DatasheetUrl)
            .HasMaxLength(500);

        // Communication Protocol
        builder.Property(s => s.Protocol)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(CommunicationProtocol.I2C);

        // Pin Configuration
        builder.Property(s => s.I2CAddress)
            .HasMaxLength(10);

        // Timing Configuration
        builder.Property(s => s.IntervalSeconds)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(s => s.MinIntervalSeconds)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.WarmupTimeMs)
            .IsRequired()
            .HasDefaultValue(0);

        // Calibration
        builder.Property(s => s.OffsetCorrection)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(s => s.GainCorrection)
            .IsRequired()
            .HasDefaultValue(1.0);

        builder.Property(s => s.CalibrationNotes)
            .HasMaxLength(1000);

        // Categorization
        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Icon)
            .HasMaxLength(50);

        builder.Property(s => s.Color)
            .HasMaxLength(10);

        // Status
        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
        builder.HasIndex(s => s.Category);
        builder.HasIndex(s => s.IsActive);
    }
}
