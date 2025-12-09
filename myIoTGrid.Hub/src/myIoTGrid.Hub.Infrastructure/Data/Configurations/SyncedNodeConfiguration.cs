using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for SyncedNode Entity.
/// Nodes synchronized from Cloud (DirectNode, VirtualNode, OtherHub).
/// </summary>
public class SyncedNodeConfiguration : IEntityTypeConfiguration<SyncedNode>
{
    public void Configure(EntityTypeBuilder<SyncedNode> builder)
    {
        builder.ToTable("SyncedNodes");

        // Primary Key
        builder.HasKey(sn => sn.Id);

        // Properties
        builder.Property(sn => sn.CloudNodeId)
            .IsRequired();

        builder.Property(sn => sn.NodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sn => sn.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sn => sn.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sn => sn.SourceDetails)
            .HasMaxLength(200);

        builder.Property(sn => sn.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sn => sn.LastSyncAt)
            .IsRequired();

        builder.Property(sn => sn.CreatedAt)
            .IsRequired();

        // Location as Owned Entity
        builder.OwnsOne(sn => sn.Location, location =>
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
        builder.HasMany(sn => sn.SyncedReadings)
            .WithOne(sr => sr.SyncedNode)
            .HasForeignKey(sr => sr.SyncedNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sn => sn.CloudNodeId).IsUnique();
        builder.HasIndex(sn => sn.NodeId);
        builder.HasIndex(sn => sn.Source);
        builder.HasIndex(sn => sn.IsOnline);
        builder.HasIndex(sn => sn.LastSyncAt);
    }
}
