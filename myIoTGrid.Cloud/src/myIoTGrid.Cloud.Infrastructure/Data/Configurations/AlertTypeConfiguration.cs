using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class AlertTypeConfiguration : IEntityTypeConfiguration<AlertType>
{
    public void Configure(EntityTypeBuilder<AlertType> builder)
    {
        builder.ToTable("AlertTypes");

        builder.HasKey(at => at.Id);

        builder.Property(at => at.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(at => at.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(at => at.Description)
            .HasMaxLength(1000);

        builder.Property(at => at.DefaultLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(at => at.IconName)
            .HasMaxLength(100);

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
