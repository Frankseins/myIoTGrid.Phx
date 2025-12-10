using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class NodeSensorAssignmentConfiguration : IEntityTypeConfiguration<NodeSensorAssignment>
{
    public void Configure(EntityTypeBuilder<NodeSensorAssignment> builder)
    {
        builder.ToTable("NodeSensorAssignments");

        builder.HasKey(nsa => nsa.Id);

        builder.Property(nsa => nsa.NodeId)
            .IsRequired();

        builder.Property(nsa => nsa.SensorId)
            .IsRequired();

        builder.Property(nsa => nsa.EndpointId)
            .IsRequired();

        builder.Property(nsa => nsa.Alias)
            .HasMaxLength(100);

        // Pin Overrides
        builder.Property(nsa => nsa.I2CAddressOverride)
            .HasMaxLength(10);

        // Status
        builder.Property(nsa => nsa.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(nsa => nsa.AssignedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(nsa => nsa.Node)
            .WithMany(n => n.SensorAssignments)
            .HasForeignKey(nsa => nsa.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nsa => nsa.Sensor)
            .WithMany(s => s.NodeAssignments)
            .HasForeignKey(nsa => nsa.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(nsa => nsa.NodeId);
        builder.HasIndex(nsa => nsa.SensorId);
        builder.HasIndex(nsa => new { nsa.NodeId, nsa.EndpointId }).IsUnique();
    }
}
