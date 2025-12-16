using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Expedition (GPS tracking session) information.
/// </summary>
public record ExpeditionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid NodeId,
    string NodeName,
    DateTime StartTime,
    DateTime EndTime,
    ExpeditionStatusDto Status,
    double? TotalDistanceKm,
    int? TotalReadings,
    double? AverageSpeedKmh,
    double? MaxSpeedKmh,
    TimeSpan Duration,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? CreatedBy,
    List<string> Tags,
    string? CoverImageUrl
);

/// <summary>
/// DTO for creating a new Expedition
/// </summary>
public record CreateExpeditionDto(
    string Name,
    string? Description,
    Guid NodeId,
    DateTime StartTime,
    DateTime EndTime,
    List<string>? Tags = null
);

/// <summary>
/// DTO for updating an existing Expedition
/// </summary>
public record UpdateExpeditionDto(
    string? Name = null,
    string? Description = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    ExpeditionStatusDto? Status = null,
    List<string>? Tags = null,
    string? CoverImageUrl = null
);

/// <summary>
/// DTO for expedition statistics (calculated from GPS readings)
/// </summary>
public record ExpeditionStatsDto(
    Guid ExpeditionId,
    string ExpeditionName,
    double TotalDistanceKm,
    int TotalReadings,
    TimeSpan Duration,
    double AverageSpeedKmh,
    double MaxSpeedKmh,
    double? StartLatitude,
    double? StartLongitude,
    double? EndLatitude,
    double? EndLongitude,
    DateTime? FirstReadingTime,
    DateTime? LastReadingTime
);

/// <summary>
/// Filter parameters for expedition list queries
/// </summary>
public record ExpeditionFilterDto(
    ExpeditionStatusDto? Status = null,
    Guid? NodeId = null,
    string? Tags = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

/// <summary>
/// DTO for a single GPS reading point with measurements
/// </summary>
public record ExpeditionGpsPointDto(
    double Latitude,
    double Longitude,
    DateTime Timestamp,
    double? Speed,
    double? Altitude,
    double? Temperature,
    double? Humidity,
    double? Pressure,
    double? WaterTemperature,
    double? Illuminance,
    int? GpsSatellites,
    int? GpsFix,
    double? Hdop
);

/// <summary>
/// DTO containing all GPS readings for an expedition
/// </summary>
public record ExpeditionGpsDataDto(
    Guid ExpeditionId,
    string ExpeditionName,
    DateTime StartTime,
    DateTime EndTime,
    List<ExpeditionGpsPointDto> Points,
    List<double[]> Trail
);
