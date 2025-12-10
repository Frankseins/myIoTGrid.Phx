using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class SyncedReadingConfiguration : IEntityTypeConfiguration<SyncedReading>
{
    public void Configure(EntityTypeBuilder<SyncedReading> builder)
    {
        builder.ToTable("SyncedReadings");

        // SyncedReading uses long Id for performance
        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.SyncedNodeId)
            .IsRequired();

        builder.Property(sr => sr.SensorCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sr => sr.MeasurementType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sr => sr.Value)
            .IsRequired();

        builder.Property(sr => sr.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.Timestamp)
            .IsRequired();

        builder.Property(sr => sr.SyncedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(sr => sr.SyncedNode)
            .WithMany(sn => sn.SyncedReadings)
            .HasForeignKey(sr => sr.SyncedNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sr => sr.SyncedNodeId);
        builder.HasIndex(sr => sr.Timestamp);
        builder.HasIndex(sr => sr.MeasurementType);
        builder.HasIndex(sr => sr.SensorCode);

        // Composite indexes for efficient querying
        builder.HasIndex(sr => new { sr.SyncedNodeId, sr.Timestamp });
    }
}
