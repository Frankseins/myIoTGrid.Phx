using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for Expedition Entity (GPS tracking session).
/// </summary>
public class ExpeditionConfiguration : IEntityTypeConfiguration<Expedition>
{
    public void Configure(EntityTypeBuilder<Expedition> builder)
    {
        builder.ToTable("Expeditions");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.NodeId)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpeditionStatus.Planned);

        builder.Property(e => e.TotalDistanceKm);

        builder.Property(e => e.TotalReadings);

        builder.Property(e => e.AverageSpeedKmh);

        builder.Property(e => e.MaxSpeedKmh);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(200);

        builder.Property(e => e.TagsJson)
            .IsRequired()
            .HasColumnName("Tags")
            .HasMaxLength(1000)
            .HasDefaultValue("[]");

        builder.Property(e => e.CoverImageUrl)
            .HasMaxLength(500);

        // Ignore computed properties
        builder.Ignore(e => e.Tags);
        builder.Ignore(e => e.Duration);

        // Relationships
        builder.HasOne(e => e.Node)
            .WithMany()
            .HasForeignKey(e => e.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.NodeId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.NodeId, e.StartTime });
    }
}
