using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Reading Entity (Measurement).
/// Matter-konform: Entspricht einem Attribute Report.
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

        builder.Property(r => r.SensorTypeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Value)
            .IsRequired();

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

        builder.HasOne(r => r.SensorType)
            .WithMany(st => st.Readings)
            .HasForeignKey(r => r.SensorTypeId)
            .HasPrincipalKey(st => st.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for Performance (time-series queries)
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.NodeId);
        builder.HasIndex(r => r.SensorTypeId);
        builder.HasIndex(r => r.Timestamp);
        builder.HasIndex(r => r.IsSyncedToCloud);
        builder.HasIndex(r => new { r.TenantId, r.Timestamp });
        builder.HasIndex(r => new { r.NodeId, r.Timestamp });
        builder.HasIndex(r => new { r.NodeId, r.SensorTypeId, r.Timestamp });
    }
}
