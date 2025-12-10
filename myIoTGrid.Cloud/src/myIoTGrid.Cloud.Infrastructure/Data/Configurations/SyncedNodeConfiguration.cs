using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class SyncedNodeConfiguration : IEntityTypeConfiguration<SyncedNode>
{
    public void Configure(EntityTypeBuilder<SyncedNode> builder)
    {
        builder.ToTable("SyncedNodes");

        builder.HasKey(sn => sn.Id);

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
            .HasConversion<int>();

        builder.Property(sn => sn.SourceDetails)
            .HasMaxLength(500);

        // Location as owned entity
        builder.OwnsOne(sn => sn.Location, location =>
        {
            location.Property(l => l.Name).HasMaxLength(200).HasColumnName("LocationName");
            location.Property(l => l.Latitude).HasColumnName("LocationLatitude");
            location.Property(l => l.Longitude).HasColumnName("LocationLongitude");
        });

        builder.Property(sn => sn.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sn => sn.LastSyncAt)
            .IsRequired();

        builder.Property(sn => sn.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(sn => sn.CloudNodeId).IsUnique();
        builder.HasIndex(sn => sn.NodeId);
        builder.HasIndex(sn => sn.Source);
        builder.HasIndex(sn => sn.IsOnline);
    }
}
