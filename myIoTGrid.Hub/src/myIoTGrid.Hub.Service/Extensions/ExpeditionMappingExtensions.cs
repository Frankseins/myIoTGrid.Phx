using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Expedition Entity.
/// </summary>
public static class ExpeditionMappingExtensions
{
    /// <summary>
    /// Converts an Expedition entity to an ExpeditionDto
    /// </summary>
    public static ExpeditionDto ToDto(this Expedition expedition)
    {
        return new ExpeditionDto(
            Id: expedition.Id,
            Name: expedition.Name,
            Description: expedition.Description,
            NodeId: expedition.NodeId,
            NodeName: expedition.Node?.Name ?? "Unknown",
            StartTime: expedition.StartTime,
            EndTime: expedition.EndTime,
            Status: expedition.Status.ToDto(),
            TotalDistanceKm: expedition.TotalDistanceKm,
            TotalReadings: expedition.TotalReadings,
            AverageSpeedKmh: expedition.AverageSpeedKmh,
            MaxSpeedKmh: expedition.MaxSpeedKmh,
            Duration: expedition.Duration,
            CreatedAt: expedition.CreatedAt,
            UpdatedAt: expedition.UpdatedAt,
            CreatedBy: expedition.CreatedBy,
            Tags: expedition.Tags,
            CoverImageUrl: expedition.CoverImageUrl
        );
    }

    /// <summary>
    /// Converts ExpeditionStatus enum to ExpeditionStatusDto
    /// </summary>
    public static ExpeditionStatusDto ToDto(this ExpeditionStatus status)
    {
        return status switch
        {
            ExpeditionStatus.Planned => ExpeditionStatusDto.Planned,
            ExpeditionStatus.Active => ExpeditionStatusDto.Active,
            ExpeditionStatus.Completed => ExpeditionStatusDto.Completed,
            ExpeditionStatus.Archived => ExpeditionStatusDto.Archived,
            _ => ExpeditionStatusDto.Planned
        };
    }

    /// <summary>
    /// Converts ExpeditionStatusDto to ExpeditionStatus enum
    /// </summary>
    public static ExpeditionStatus ToEntity(this ExpeditionStatusDto status)
    {
        return status switch
        {
            ExpeditionStatusDto.Planned => ExpeditionStatus.Planned,
            ExpeditionStatusDto.Active => ExpeditionStatus.Active,
            ExpeditionStatusDto.Completed => ExpeditionStatus.Completed,
            ExpeditionStatusDto.Archived => ExpeditionStatus.Archived,
            _ => ExpeditionStatus.Planned
        };
    }

    /// <summary>
    /// Converts a CreateExpeditionDto to an Expedition entity
    /// </summary>
    public static Expedition ToEntity(this CreateExpeditionDto dto)
    {
        return new Expedition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            NodeId = dto.NodeId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = CalculateStatus(dto.StartTime, dto.EndTime),
            Tags = dto.Tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies an UpdateExpeditionDto to an Expedition entity
    /// </summary>
    public static void ApplyUpdate(this Expedition expedition, UpdateExpeditionDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name))
            expedition.Name = dto.Name;

        if (dto.Description != null)
            expedition.Description = dto.Description;

        if (dto.StartTime.HasValue)
            expedition.StartTime = dto.StartTime.Value;

        if (dto.EndTime.HasValue)
            expedition.EndTime = dto.EndTime.Value;

        if (dto.Status.HasValue)
            expedition.Status = dto.Status.Value.ToEntity();

        if (dto.Tags != null)
            expedition.Tags = dto.Tags;

        if (dto.CoverImageUrl != null)
            expedition.CoverImageUrl = dto.CoverImageUrl;

        expedition.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the expedition status based on the time range
    /// </summary>
    public static ExpeditionStatus CalculateStatus(DateTime startTime, DateTime endTime)
    {
        var now = DateTime.UtcNow;

        if (now < startTime)
            return ExpeditionStatus.Planned;

        if (now >= startTime && now <= endTime)
            return ExpeditionStatus.Active;

        return ExpeditionStatus.Completed;
    }
}
