using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myIoTGrid.Shared.Common.Entities;

namespace myIoTGrid.Cloud.Infrastructure.Data.Configurations;

public class NodeDebugLogConfiguration : IEntityTypeConfiguration<NodeDebugLog>
{
    public void Configure(EntityTypeBuilder<NodeDebugLog> builder)
    {
        builder.ToTable("NodeDebugLogs");

        builder.HasKey(ndl => ndl.Id);

        builder.Property(ndl => ndl.NodeId)
            .IsRequired();

        builder.Property(ndl => ndl.NodeTimestamp)
            .IsRequired();

        builder.Property(ndl => ndl.ReceivedAt)
            .IsRequired();

        builder.Property(ndl => ndl.Level)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ndl => ndl.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ndl => ndl.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(ndl => ndl.StackTrace)
            .HasMaxLength(8000);

        // Relationships
        builder.HasOne(ndl => ndl.Node)
            .WithMany(n => n.DebugLogs)
            .HasForeignKey(ndl => ndl.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ndl => ndl.NodeId);
        builder.HasIndex(ndl => ndl.ReceivedAt);
        builder.HasIndex(ndl => ndl.Level);
        builder.HasIndex(ndl => ndl.Category);

        // Composite index for node debug log queries
        builder.HasIndex(ndl => new { ndl.NodeId, ndl.ReceivedAt });
    }
}
