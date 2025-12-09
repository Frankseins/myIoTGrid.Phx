using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for SyncedReading Entity (v3.0).
/// Readings synchronized from Cloud.
/// </summary>
public class SyncedReadingConfiguration : IEntityTypeConfiguration<SyncedReading>
{
    public void Configure(EntityTypeBuilder<SyncedReading> builder)
    {
        builder.ToTable("SyncedReadings");

        // Primary Key - using long Id for performance (auto-increment)
        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(sr => sr.SyncedNodeId)
            .IsRequired();

        builder.Property(sr => sr.SensorCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.MeasurementType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.Value)
            .IsRequired();

        builder.Property(sr => sr.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(sr => sr.Timestamp)
            .IsRequired();

        builder.Property(sr => sr.SyncedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(sr => sr.SyncedNode)
            .WithMany(sn => sn.SyncedReadings)
            .HasForeignKey(sr => sr.SyncedNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for Performance (time-series queries)
        builder.HasIndex(sr => sr.SyncedNodeId);
        builder.HasIndex(sr => sr.SensorCode);
        builder.HasIndex(sr => sr.MeasurementType);
        builder.HasIndex(sr => sr.Timestamp);
        builder.HasIndex(sr => sr.SyncedAt);
        builder.HasIndex(sr => new { sr.SyncedNodeId, sr.Timestamp });
        builder.HasIndex(sr => new { sr.SyncedNodeId, sr.SensorCode, sr.Timestamp });
    }
}
