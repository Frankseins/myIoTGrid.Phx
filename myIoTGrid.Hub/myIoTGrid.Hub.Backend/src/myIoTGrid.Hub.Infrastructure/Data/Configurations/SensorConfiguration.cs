using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Sensor Entity (Physical sensor chip: DHT22, BME280).
/// Matter-konform: Entspricht einem Matter Endpoint.
/// </summary>
public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");

        // Primary Key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.NodeId)
            .IsRequired();

        builder.Property(s => s.SensorTypeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.EndpointId)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Node)
            .WithMany(n => n.Sensors)
            .HasForeignKey(s => s.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.SensorType)
            .WithMany(st => st.Sensors)
            .HasForeignKey(s => s.SensorTypeId)
            .HasPrincipalKey(st => st.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => s.NodeId);
        builder.HasIndex(s => s.SensorTypeId);
        builder.HasIndex(s => new { s.NodeId, s.EndpointId }).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}
