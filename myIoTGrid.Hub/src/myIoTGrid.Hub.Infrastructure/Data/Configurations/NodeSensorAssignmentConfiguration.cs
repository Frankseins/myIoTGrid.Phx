using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for NodeSensorAssignment Entity.
/// Hardware binding of a Sensor to a Node with pin configuration.
/// </summary>
public class NodeSensorAssignmentConfiguration : IEntityTypeConfiguration<NodeSensorAssignment>
{
    public void Configure(EntityTypeBuilder<NodeSensorAssignment> builder)
    {
        builder.ToTable("NodeSensorAssignments");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.NodeId)
            .IsRequired();

        builder.Property(a => a.SensorId)
            .IsRequired();

        builder.Property(a => a.EndpointId)
            .IsRequired();

        builder.Property(a => a.Alias)
            .HasMaxLength(200);

        // Pin Overrides
        builder.Property(a => a.I2CAddressOverride)
            .HasMaxLength(10);

        // Flags
        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(a => a.AssignedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(a => a.Node)
            .WithMany(n => n.SensorAssignments)
            .HasForeignKey(a => a.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Sensor)
            .WithMany(s => s.NodeAssignments)
            .HasForeignKey(a => a.SensorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Readings)
            .WithOne(r => r.Assignment)
            .HasForeignKey(r => r.AssignmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(a => a.NodeId);
        builder.HasIndex(a => a.SensorId);
        builder.HasIndex(a => new { a.NodeId, a.EndpointId }).IsUnique();
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.LastSeenAt);
    }
}
