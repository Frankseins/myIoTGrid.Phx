using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for SensorType Entity.
/// Matter-konform: Entspricht einem Matter Cluster.
/// </summary>
public class SensorTypeConfiguration : IEntityTypeConfiguration<SensorType>
{
    public void Configure(EntityTypeBuilder<SensorType> builder)
    {
        builder.ToTable("SensorTypes");

        // Primary Key - TypeId is the primary key (not Guid Id)
        builder.HasKey(st => st.TypeId);

        // Properties
        builder.Property(st => st.TypeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(st => st.ClusterId)
            .IsRequired();

        builder.Property(st => st.MatterClusterName)
            .HasMaxLength(100);

        builder.Property(st => st.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(st => st.Resolution)
            .IsRequired()
            .HasDefaultValue(0.1);

        builder.Property(st => st.Description)
            .HasMaxLength(500);

        builder.Property(st => st.IsCustom)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(st => st.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("other");

        builder.Property(st => st.Icon)
            .HasMaxLength(50);

        builder.Property(st => st.Color)
            .HasMaxLength(20);

        builder.Property(st => st.IsGlobal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(st => st.CreatedAt)
            .IsRequired();

        // Ignore the IEntity.Id property as we use TypeId as primary key
        builder.Ignore(st => ((Domain.Interfaces.IEntity)st).Id);

        // Indexes
        builder.HasIndex(st => st.ClusterId);
        builder.HasIndex(st => st.Category);
        builder.HasIndex(st => st.IsGlobal);
        builder.HasIndex(st => st.IsCustom);
    }
}
