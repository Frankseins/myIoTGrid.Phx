using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.Entities;
using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Expedition (GPS tracking session) management.
/// Allows users to save, organize and analyze their GPS routes.
/// </summary>
public class ExpeditionService : IExpeditionService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpeditionService> _logger;

    public ExpeditionService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<ExpeditionService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExpeditionDto> CreateAsync(CreateExpeditionDto dto, CancellationToken ct = default)
    {
        // Validate time range
        if (dto.StartTime >= dto.EndTime)
            throw new ArgumentException("StartTime must be before EndTime");

        // Validate node exists
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == dto.NodeId, ct);

        if (node == null)
            throw new ArgumentException($"Node with ID {dto.NodeId} not found");

        var expedition = dto.ToEntity();

        _context.Expeditions.Add(expedition);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created expedition {ExpeditionId} '{Name}' for Node {NodeId}",
            expedition.Id, expedition.Name, expedition.NodeId);

        // Calculate initial statistics
        await CalculateAndUpdateStatisticsAsync(expedition.Id, ct);

        return await GetByIdAsync(expedition.Id, ct)
            ?? throw new InvalidOperationException("Failed to retrieve created expedition");
    }

    /// <inheritdoc />
    public async Task<ExpeditionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .AsNoTracking()
            .Include(e => e.Node)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return expedition?.ToDto();
    }

    /// <inheritdoc />
    public async Task<List<ExpeditionDto>> GetAllAsync(ExpeditionFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = _context.Expeditions
            .AsNoTracking()
            .Include(e => e.Node)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.Status.HasValue)
                query = query.Where(e => e.Status == filter.Status.Value.ToEntity());

            if (filter.NodeId.HasValue)
                query = query.Where(e => e.NodeId == filter.NodeId.Value);

            if (!string.IsNullOrEmpty(filter.Tags))
            {
                var tagList = filter.Tags.Split(',').Select(t => t.Trim()).ToList();
                query = query.Where(e => tagList.Any(tag => e.TagsJson.Contains(tag)));
            }

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.StartTime >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.EndTime <= filter.ToDate.Value);
        }

        var expeditions = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        return expeditions.Select(e => e.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ExpeditionDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        var expeditions = await _context.Expeditions
            .AsNoTracking()
            .Include(e => e.Node)
            .Where(e => e.NodeId == nodeId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        return expeditions.Select(e => e.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<ExpeditionDto?> UpdateAsync(Guid id, UpdateExpeditionDto dto, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .Include(e => e.Node)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expedition == null)
            return null;

        // Validate time range if both are provided
        var newStart = dto.StartTime ?? expedition.StartTime;
        var newEnd = dto.EndTime ?? expedition.EndTime;
        if (newStart >= newEnd)
            throw new ArgumentException("StartTime must be before EndTime");

        expedition.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated expedition {ExpeditionId}", id);

        // Recalculate statistics if time range changed
        if (dto.StartTime.HasValue || dto.EndTime.HasValue)
        {
            await CalculateAndUpdateStatisticsAsync(id, ct);
        }

        return await GetByIdAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expedition == null)
            return false;

        _context.Expeditions.Remove(expedition);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted expedition {ExpeditionId}", id);

        return true;
    }

    /// <inheritdoc />
    public async Task<ExpeditionStatsDto?> GetStatisticsAsync(Guid id, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .AsNoTracking()
            .Include(e => e.Node)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expedition == null)
            return null;

        // Get GPS readings for this expedition's time range
        var readings = await GetGpsReadingsForExpedition(expedition, ct);

        if (!readings.Any())
        {
            return new ExpeditionStatsDto(
                ExpeditionId: expedition.Id,
                ExpeditionName: expedition.Name,
                TotalDistanceKm: 0,
                TotalReadings: 0,
                Duration: expedition.Duration,
                AverageSpeedKmh: 0,
                MaxSpeedKmh: 0,
                StartLatitude: null,
                StartLongitude: null,
                EndLatitude: null,
                EndLongitude: null,
                FirstReadingTime: null,
                LastReadingTime: null
            );
        }

        var (totalDistance, avgSpeed, maxSpeed) = CalculateGpsMetrics(readings);

        var firstReading = readings.First();
        var lastReading = readings.Last();

        return new ExpeditionStatsDto(
            ExpeditionId: expedition.Id,
            ExpeditionName: expedition.Name,
            TotalDistanceKm: totalDistance,
            TotalReadings: readings.Count,
            Duration: expedition.Duration,
            AverageSpeedKmh: avgSpeed,
            MaxSpeedKmh: maxSpeed,
            StartLatitude: firstReading.Latitude,
            StartLongitude: firstReading.Longitude,
            EndLatitude: lastReading.Latitude,
            EndLongitude: lastReading.Longitude,
            FirstReadingTime: firstReading.Timestamp,
            LastReadingTime: lastReading.Timestamp
        );
    }

    /// <inheritdoc />
    public async Task<ExpeditionDto?> RecalculateStatisticsAsync(Guid id, CancellationToken ct = default)
    {
        await CalculateAndUpdateStatisticsAsync(id, ct);
        return await GetByIdAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<ExpeditionDto?> UpdateStatusAsync(Guid id, ExpeditionStatusDto status, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expedition == null)
            return null;

        expedition.Status = status.ToEntity();
        expedition.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated expedition {ExpeditionId} status to {Status}", id, status);

        return await GetByIdAsync(id, ct);
    }

    #region Private Helper Methods

    private async Task CalculateAndUpdateStatisticsAsync(Guid expeditionId, CancellationToken ct)
    {
        var expedition = await _context.Expeditions
            .FirstOrDefaultAsync(e => e.Id == expeditionId, ct);

        if (expedition == null)
            return;

        var readings = await GetGpsReadingsForExpedition(expedition, ct);

        if (!readings.Any())
        {
            expedition.TotalDistanceKm = 0;
            expedition.TotalReadings = 0;
            expedition.AverageSpeedKmh = 0;
            expedition.MaxSpeedKmh = 0;
        }
        else
        {
            var (totalDistance, avgSpeed, maxSpeed) = CalculateGpsMetrics(readings);

            expedition.TotalDistanceKm = totalDistance;
            expedition.TotalReadings = readings.Count;
            expedition.AverageSpeedKmh = avgSpeed;
            expedition.MaxSpeedKmh = maxSpeed;
        }

        expedition.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug("Calculated statistics for expedition {ExpeditionId}: {Distance}km, {Readings} readings",
            expeditionId, expedition.TotalDistanceKm, expedition.TotalReadings);
    }

    private async Task<List<GpsReading>> GetGpsReadingsForExpedition(Expedition expedition, CancellationToken ct)
    {
        // Find sensor assignments that have GPS capabilities (latitude/longitude)
        var gpsAssignments = await _context.NodeSensorAssignments
            .AsNoTracking()
            .Where(nsa => nsa.NodeId == expedition.NodeId)
            .Include(nsa => nsa.Sensor)
                .ThenInclude(s => s!.Capabilities)
            .Where(nsa => nsa.Sensor!.Capabilities.Any(c =>
                c.MeasurementType == "latitude" || c.MeasurementType == "longitude"))
            .Select(nsa => nsa.Id)
            .ToListAsync(ct);

        if (!gpsAssignments.Any())
            return new List<GpsReading>();

        // Get all GPS-related readings in the time range
        var readings = await _context.Readings
            .AsNoTracking()
            .Where(r => r.NodeId == expedition.NodeId
                && r.Timestamp >= expedition.StartTime
                && r.Timestamp <= expedition.EndTime
                && r.AssignmentId.HasValue
                && gpsAssignments.Contains(r.AssignmentId.Value))
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        // Group readings by timestamp to get lat/lon pairs
        var gpsReadings = readings
            .GroupBy(r => new { r.Timestamp.Date, Hour = r.Timestamp.Hour, Minute = r.Timestamp.Minute, Second = r.Timestamp.Second })
            .Select(g =>
            {
                var lat = g.FirstOrDefault(r => r.MeasurementType == "latitude")?.Value;
                var lon = g.FirstOrDefault(r => r.MeasurementType == "longitude")?.Value;
                var speed = g.FirstOrDefault(r => r.MeasurementType == "speed")?.Value;
                var timestamp = g.First().Timestamp;

                return new GpsReading
                {
                    Latitude = lat,
                    Longitude = lon,
                    Speed = speed,
                    Timestamp = timestamp
                };
            })
            .Where(g => g.Latitude.HasValue && g.Longitude.HasValue)
            .OrderBy(g => g.Timestamp)
            .ToList();

        return gpsReadings;
    }

    private (double TotalDistance, double AvgSpeed, double MaxSpeed) CalculateGpsMetrics(List<GpsReading> readings)
    {
        if (readings.Count < 2)
            return (0, 0, readings.FirstOrDefault()?.Speed ?? 0);

        double totalDistance = 0;
        double maxSpeed = 0;

        for (int i = 1; i < readings.Count; i++)
        {
            var prev = readings[i - 1];
            var curr = readings[i];

            if (prev.Latitude.HasValue && prev.Longitude.HasValue &&
                curr.Latitude.HasValue && curr.Longitude.HasValue)
            {
                var distance = CalculateHaversineDistance(
                    prev.Latitude.Value, prev.Longitude.Value,
                    curr.Latitude.Value, curr.Longitude.Value);
                totalDistance += distance;
            }

            if (curr.Speed.HasValue && curr.Speed.Value > maxSpeed)
                maxSpeed = curr.Speed.Value;
        }

        // Calculate average speed based on total distance and time
        var firstReading = readings.First();
        var lastReading = readings.Last();
        var totalHours = (lastReading.Timestamp - firstReading.Timestamp).TotalHours;
        var avgSpeed = totalHours > 0 ? totalDistance / totalHours : 0;

        return (Math.Round(totalDistance, 2), Math.Round(avgSpeed, 2), Math.Round(maxSpeed, 2));
    }

    /// <summary>
    /// Calculates the distance between two GPS coordinates using the Haversine formula.
    /// Returns distance in kilometers.
    /// </summary>
    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    #endregion

    #region Helper Classes

    private class GpsReading
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Altitude { get; set; }
        public double? Temperature { get; set; }
        public double? Humidity { get; set; }
        public double? Pressure { get; set; }
        public double? WaterTemperature { get; set; }
        public double? Illuminance { get; set; }
        public int? GpsSatellites { get; set; }
        public int? GpsFix { get; set; }
        public double? Hdop { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion

    /// <inheritdoc />
    public async Task<ExpeditionGpsDataDto?> GetGpsDataAsync(Guid id, CancellationToken ct = default)
    {
        var expedition = await _context.Expeditions
            .AsNoTracking()
            .Include(e => e.Node)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (expedition == null)
            return null;

        var readings = await GetFullGpsReadingsForExpedition(expedition, ct);

        var points = readings.Select(r => new ExpeditionGpsPointDto(
            Latitude: r.Latitude!.Value,
            Longitude: r.Longitude!.Value,
            Timestamp: r.Timestamp,
            Speed: r.Speed,
            Altitude: r.Altitude,
            Temperature: r.Temperature,
            Humidity: r.Humidity,
            Pressure: r.Pressure,
            WaterTemperature: r.WaterTemperature,
            Illuminance: r.Illuminance,
            GpsSatellites: r.GpsSatellites,
            GpsFix: r.GpsFix,
            Hdop: r.Hdop
        )).ToList();

        var trail = readings
            .Select(r => new[] { r.Latitude!.Value, r.Longitude!.Value })
            .ToList();

        return new ExpeditionGpsDataDto(
            ExpeditionId: expedition.Id,
            ExpeditionName: expedition.Name,
            StartTime: expedition.StartTime,
            EndTime: expedition.EndTime,
            Points: points,
            Trail: trail
        );
    }

    private async Task<List<GpsReading>> GetFullGpsReadingsForExpedition(Expedition expedition, CancellationToken ct)
    {
        // Get all readings for this node in the time range
        var readings = await _context.Readings
            .AsNoTracking()
            .Where(r => r.NodeId == expedition.NodeId
                && r.Timestamp >= expedition.StartTime
                && r.Timestamp <= expedition.EndTime)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        // Group readings by timestamp (within same second) to combine different measurement types
        var gpsReadings = readings
            .GroupBy(r => new { r.Timestamp.Date, Hour = r.Timestamp.Hour, Minute = r.Timestamp.Minute, Second = r.Timestamp.Second })
            .Select(g =>
            {
                var lat = g.FirstOrDefault(r => r.MeasurementType == "latitude")?.Value;
                var lon = g.FirstOrDefault(r => r.MeasurementType == "longitude")?.Value;

                if (!lat.HasValue || !lon.HasValue)
                    return null;

                return new GpsReading
                {
                    Latitude = lat,
                    Longitude = lon,
                    Speed = g.FirstOrDefault(r => r.MeasurementType == "speed")?.Value,
                    Altitude = g.FirstOrDefault(r => r.MeasurementType == "altitude")?.Value,
                    Temperature = g.FirstOrDefault(r => r.MeasurementType == "temperature")?.Value,
                    Humidity = g.FirstOrDefault(r => r.MeasurementType == "humidity")?.Value,
                    Pressure = g.FirstOrDefault(r => r.MeasurementType == "pressure")?.Value,
                    WaterTemperature = g.FirstOrDefault(r => r.MeasurementType == "waterTemperature")?.Value,
                    Illuminance = g.FirstOrDefault(r => r.MeasurementType == "illuminance")?.Value,
                    GpsSatellites = (int?)g.FirstOrDefault(r => r.MeasurementType == "gpsSatellites")?.Value,
                    GpsFix = (int?)g.FirstOrDefault(r => r.MeasurementType == "gpsFix")?.Value,
                    Hdop = g.FirstOrDefault(r => r.MeasurementType == "hdop")?.Value,
                    Timestamp = g.First().Timestamp
                };
            })
            .Where(g => g != null)
            .Cast<GpsReading>()
            .OrderBy(g => g.Timestamp)
            .ToList();

        return gpsReadings;
    }
}
