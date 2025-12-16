using System.Text.Json;
using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Represents a GPS tracking session (expedition).
/// Allows users to save and organize their GPS routes with metadata.
/// </summary>
public class Expedition : IEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>Display name of the expedition (e.g., "Erftmündung Expedition")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>FK to the Node that recorded this expedition</summary>
    public Guid NodeId { get; set; }

    /// <summary>Start time of the expedition</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End time of the expedition</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Current status of the expedition</summary>
    public ExpeditionStatus Status { get; set; } = ExpeditionStatus.Planned;

    /// <summary>Total distance traveled in kilometers (calculated from GPS readings)</summary>
    public double? TotalDistanceKm { get; set; }

    /// <summary>Total number of GPS readings in this expedition</summary>
    public int? TotalReadings { get; set; }

    /// <summary>Average speed in km/h</summary>
    public double? AverageSpeedKmh { get; set; }

    /// <summary>Maximum speed in km/h</summary>
    public double? MaxSpeedKmh { get; set; }

    /// <summary>When the expedition was created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the expedition was last updated</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>User who created the expedition</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Tags for categorization (stored as JSON)</summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>Cover image URL (optional)</summary>
    public string? CoverImageUrl { get; set; }

    // === Navigation Properties ===

    /// <summary>Node that recorded this expedition</summary>
    public Node? Node { get; set; }

    // === Computed Properties ===

    /// <summary>Tags as a list (deserialized from JSON)</summary>
    public List<string> Tags
    {
        get => string.IsNullOrEmpty(TagsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value ?? new List<string>());
    }

    /// <summary>Duration of the expedition</summary>
    public TimeSpan Duration => EndTime - StartTime;
}
