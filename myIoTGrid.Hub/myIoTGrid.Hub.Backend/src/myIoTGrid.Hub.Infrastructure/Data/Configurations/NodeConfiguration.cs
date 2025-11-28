using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Node Entity (ESP32/LoRa32 Device).
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
public class NodeConfiguration : IEntityTypeConfiguration<Node>
{
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("Nodes");

        // Primary Key
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.HubId)
            .IsRequired();

        builder.Property(n => n.NodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Protocol)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(n => n.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Location as Owned Entity
        builder.OwnsOne(n => n.Location, location =>
        {
            location.Property(l => l.Name)
                .HasColumnName("Location_Name")
                .HasMaxLength(200);

            location.Property(l => l.Latitude)
                .HasColumnName("Location_Latitude");

            location.Property(l => l.Longitude)
                .HasColumnName("Location_Longitude");
        });

        // Relationships
        builder.HasOne(n => n.Hub)
            .WithMany(h => h.Nodes)
            .HasForeignKey(n => n.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Sensors)
            .WithOne(s => s.Node)
            .HasForeignKey(s => s.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Readings)
            .WithOne(r => r.Node)
            .HasForeignKey(r => r.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(n => n.HubId);
        builder.HasIndex(n => n.NodeId);
        builder.HasIndex(n => new { n.HubId, n.NodeId }).IsUnique();
        builder.HasIndex(n => n.IsOnline);
        builder.HasIndex(n => n.LastSeen);
    }
}
