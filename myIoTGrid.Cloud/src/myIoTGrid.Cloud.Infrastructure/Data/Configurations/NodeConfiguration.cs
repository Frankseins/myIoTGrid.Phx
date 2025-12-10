using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class NodeConfiguration : IEntityTypeConfiguration<Node>
{
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("Nodes");

        builder.HasKey(n => n.Id);

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
            .HasConversion<int>()
            .HasDefaultValue(Protocol.WLAN);

        // Location as owned entity
        builder.OwnsOne(n => n.Location, loc =>
        {
            loc.Property(l => l.Name).HasColumnName("LocationName").HasMaxLength(200);
            loc.Property(l => l.Latitude).HasColumnName("LocationLatitude");
            loc.Property(l => l.Longitude).HasColumnName("LocationLongitude");
        });

        builder.Property(n => n.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(n => n.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Provisioning
        builder.Property(n => n.MacAddress)
            .IsRequired()
            .HasMaxLength(17);

        builder.Property(n => n.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(NodeStatus.Unconfigured);

        builder.Property(n => n.IsSimulation)
            .IsRequired()
            .HasDefaultValue(false);

        // Offline Storage
        builder.Property(n => n.StorageMode)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(StorageMode.RemoteOnly);

        builder.Property(n => n.PendingSyncCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.LastSyncError)
            .HasMaxLength(500);

        // Remote Debug System
        builder.Property(n => n.DebugLevel)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(DebugLevel.Normal);

        builder.Property(n => n.EnableRemoteLogging)
            .IsRequired()
            .HasDefaultValue(false);

        // Hardware Status
        builder.Property(n => n.HardwareStatusJson)
            .HasColumnType("jsonb");

        // Relationships
        builder.HasOne(n => n.Hub)
            .WithMany(h => h.Nodes)
            .HasForeignKey(n => n.HubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(n => n.HubId);
        builder.HasIndex(n => new { n.HubId, n.NodeId }).IsUnique();
        builder.HasIndex(n => n.MacAddress);
        builder.HasIndex(n => n.IsOnline);
        builder.HasIndex(n => n.Status);
    }
}
