using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace myIoTGrid.Hub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Configuration for NodeDebugLog Entity.
/// Sprint 8: Remote Debug System - stores debug logs from nodes.
/// </summary>
public class NodeDebugLogConfiguration : IEntityTypeConfiguration<NodeDebugLog>
{
    public void Configure(EntityTypeBuilder<NodeDebugLog> builder)
    {
        builder.ToTable("NodeDebugLogs");

        // Primary Key
        builder.HasKey(l => l.Id);

        // Properties
        builder.Property(l => l.NodeId)
            .IsRequired();

        builder.Property(l => l.NodeTimestamp)
            .IsRequired();

        builder.Property(l => l.ReceivedAt)
            .IsRequired();

        builder.Property(l => l.Level)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(l => l.StackTrace)
            .HasMaxLength(8000);

        // Relationships
        builder.HasOne(l => l.Node)
            .WithMany(n => n.DebugLogs)
            .HasForeignKey(l => l.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for Performance (time-series queries and filtering)
        builder.HasIndex(l => l.NodeId);
        builder.HasIndex(l => l.ReceivedAt);
        builder.HasIndex(l => l.Level);
        builder.HasIndex(l => l.Category);
        builder.HasIndex(l => new { l.NodeId, l.ReceivedAt });
        builder.HasIndex(l => new { l.NodeId, l.Level, l.ReceivedAt });
        builder.HasIndex(l => new { l.NodeId, l.Category, l.ReceivedAt });
    }
}
