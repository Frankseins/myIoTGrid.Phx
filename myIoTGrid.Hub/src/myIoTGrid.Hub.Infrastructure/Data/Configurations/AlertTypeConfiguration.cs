using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration für AlertType Entity
/// </summary>
public class AlertTypeConfiguration : IEntityTypeConfiguration<AlertType>
{
    public void Configure(EntityTypeBuilder<AlertType> builder)
    {
        builder.ToTable("AlertTypes");

        // Primary Key
        builder.HasKey(at => at.Id);

        // Properties
        builder.Property(at => at.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(at => at.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(at => at.Description)
            .HasMaxLength(500);

        builder.Property(at => at.DefaultLevel)
            .IsRequired();

        builder.Property(at => at.IconName)
            .HasMaxLength(50);

        builder.Property(at => at.IsGlobal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(at => at.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(at => at.Code).IsUnique();
        builder.HasIndex(at => at.IsGlobal);
    }
}
