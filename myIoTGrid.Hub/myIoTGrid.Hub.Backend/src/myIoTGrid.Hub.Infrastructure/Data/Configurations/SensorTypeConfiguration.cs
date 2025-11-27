using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Hub.Domain.Entities;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration für SensorType Entity
/// </summary>
public class SensorTypeConfiguration : IEntityTypeConfiguration<SensorType>
{
    public void Configure(EntityTypeBuilder<SensorType> builder)
    {
        builder.ToTable("SensorTypes");

        // Primary Key
        builder.HasKey(st => st.Id);

        // Properties
        builder.Property(st => st.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(st => st.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(st => st.Description)
            .HasMaxLength(500);

        builder.Property(st => st.IconName)
            .HasMaxLength(50);

        builder.Property(st => st.IsGlobal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(st => st.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(st => st.Code).IsUnique();
        builder.HasIndex(st => st.IsGlobal);
    }
}
