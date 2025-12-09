using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for chart data aggregation and readings list.
/// </summary>
public class ChartService : IChartService
{
    private readonly HubDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChartService> _logger;

    // Default colors for measurement types
    private static readonly Dictionary<string, string> MeasurementTypeColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = "#FF5722",
        ["water_temperature"] = "#00BCD4",
        ["humidity"] = "#2196F3",
        ["pressure"] = "#9C27B0",
        ["co2"] = "#4CAF50",
        ["pm25"] = "#795548",
        ["pm10"] = "#607D8B",
        ["soil_moisture"] = "#8BC34A",
        ["light"] = "#FFC107",
        ["illuminance"] = "#FFC107",
        ["uv"] = "#E91E63",
        ["wind_speed"] = "#00BCD4",
        ["rainfall"] = "#3F51B5",
        ["water_level"] = "#009688",
        ["battery"] = "#CDDC39",
        ["rssi"] = "#9E9E9E"
    };

    public ChartService(
        HubDbContext context,
        ITenantService tenantService,
        ILogger<ChartService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChartDataDto?> GetChartDataAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        ChartInterval interval,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var (startTime, aggregationMinutes, _) = GetIntervalParameters(interval);

        _logger.LogDebug("Getting chart data for node {NodeId}, assignment {AssignmentId}, type {Type}, interval {Interval}",
            nodeId, assignmentId, measurementType, interval);

        // Get node with location and sensor assignment
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.Location)
            .Include(n => n.SensorAssignments)
                .ThenInclude(a => a.Sensor)
                    .ThenInclude(s => s!.Capabilities)
            .Where(n => n.Hub!.TenantId == tenantId && n.Id == nodeId)
            .FirstOrDefaultAsync(ct);

        if (node == null)
        {
            _logger.LogWarning("Node {NodeId} not found", nodeId);
            return null;
        }

        var assignment = node.SensorAssignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment == null)
        {
            _logger.LogWarning("Assignment {AssignmentId} not found for node {NodeId}", assignmentId, nodeId);
            return null;
        }

        // Get readings for this node, assignment, and measurement type
        var readings = await _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                     && r.NodeId == nodeId
                     && r.AssignmentId == assignmentId
                     && r.MeasurementType.ToLower() == measurementType.ToLower()
                     && r.Timestamp >= startTime)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        if (!readings.Any())
        {
            _logger.LogDebug("No readings found for the specified period");
            return null;
        }

        // Aggregate data points
        var dataPoints = AggregateReadings(readings, aggregationMinutes);

        // Calculate stats
        var stats = CalculateStats(readings);

        // Calculate trend
        var trend = CalculateTrend(readings, interval);

        // Get latest reading
        var latestReading = readings.Last();

        // Get sensor info
        var sensorName = assignment.Sensor?.Name ?? "";
        var color = assignment.Sensor?.Color ?? GetDefaultColor(measurementType);
        var locationName = node.Location?.Name ?? "Unbekannt";

        return new ChartDataDto(
            NodeId: nodeId,
            NodeName: node.Name,
            AssignmentId: assignmentId,
            SensorId: assignment.SensorId,
            SensorName: sensorName,
            MeasurementType: measurementType.ToLowerInvariant(),
            LocationName: locationName,
            Unit: latestReading.Unit,
            Color: color,
            CurrentValue: Math.Round(latestReading.Value, 2),
            LastUpdate: latestReading.Timestamp,
            Stats: stats,
            Trend: trend,
            DataPoints: dataPoints
        );
    }

    /// <inheritdoc />
    public async Task<ReadingsListDto> GetReadingsListAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        ReadingsListRequestDto request,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                     && r.NodeId == nodeId
                     && r.AssignmentId == assignmentId
                     && r.MeasurementType.ToLower() == measurementType.ToLower());

        // Apply date filters
        if (request.From.HasValue)
        {
            query = query.Where(r => r.Timestamp >= request.From.Value);
        }
        if (request.To.HasValue)
        {
            query = query.Where(r => r.Timestamp <= request.To.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Get paginated results (newest first)
        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // Map to DTOs with trend
        var items = new List<ReadingListItemDto>();
        for (var i = 0; i < readings.Count; i++)
        {
            var reading = readings[i];
            string? trendDirection = null;

            if (i < readings.Count - 1)
            {
                var nextReading = readings[i + 1];
                if (reading.Value > nextReading.Value + 0.1)
                    trendDirection = "up";
                else if (reading.Value < nextReading.Value - 0.1)
                    trendDirection = "down";
                else
                    trendDirection = "stable";
            }

            items.Add(new ReadingListItemDto(
                Id: reading.Id,
                Timestamp: reading.Timestamp,
                Value: Math.Round(reading.Value, 2),
                Unit: reading.Unit,
                TrendDirection: trendDirection
            ));
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new ReadingsListDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToCsvAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                     && r.NodeId == nodeId
                     && r.AssignmentId == assignmentId
                     && r.MeasurementType.ToLower() == measurementType.ToLower());

        if (from.HasValue)
        {
            query = query.Where(r => r.Timestamp >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(r => r.Timestamp <= to.Value);
        }

        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Zeitstempel;Wert;Einheit");

        foreach (var reading in readings)
        {
            sb.AppendLine($"{reading.Timestamp:dd.MM.yyyy HH:mm:ss};{reading.Value.ToString("F2", CultureInfo.InvariantCulture)};{reading.Unit}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static (DateTime startTime, int aggregationMinutes, int maxPoints) GetIntervalParameters(ChartInterval interval)
    {
        return interval switch
        {
            ChartInterval.OneHour => (DateTime.UtcNow.AddHours(-1), 1, 60),
            ChartInterval.OneDay => (DateTime.UtcNow.AddDays(-1), 15, 96),
            ChartInterval.OneWeek => (DateTime.UtcNow.AddDays(-7), 60, 168),
            ChartInterval.OneMonth => (DateTime.UtcNow.AddMonths(-1), 360, 124),
            ChartInterval.ThreeMonths => (DateTime.UtcNow.AddMonths(-3), 1440, 92),
            ChartInterval.SixMonths => (DateTime.UtcNow.AddMonths(-6), 1440, 183),
            ChartInterval.OneYear => (DateTime.UtcNow.AddYears(-1), 10080, 52),
            _ => (DateTime.UtcNow.AddDays(-1), 15, 96)
        };
    }

    private static List<ChartPointDto> AggregateReadings(List<Reading> readings, int intervalMinutes)
    {
        if (!readings.Any()) return new List<ChartPointDto>();

        var grouped = readings
            .GroupBy(r => new DateTime(
                r.Timestamp.Year,
                r.Timestamp.Month,
                r.Timestamp.Day,
                r.Timestamp.Hour,
                (r.Timestamp.Minute / intervalMinutes) * intervalMinutes,
                0,
                DateTimeKind.Utc))
            .OrderBy(g => g.Key)
            .Select(g => new ChartPointDto(
                Timestamp: g.Key,
                Value: Math.Round(g.Average(r => r.Value), 2),
                Min: g.Count() > 1 ? Math.Round(g.Min(r => r.Value), 2) : null,
                Max: g.Count() > 1 ? Math.Round(g.Max(r => r.Value), 2) : null
            ))
            .ToList();

        return grouped;
    }

    private static ChartStatsDto CalculateStats(List<Reading> readings)
    {
        if (!readings.Any())
        {
            return new ChartStatsDto(0, DateTime.UtcNow, 0, DateTime.UtcNow, 0);
        }

        var minReading = readings.MinBy(r => r.Value)!;
        var maxReading = readings.MaxBy(r => r.Value)!;
        var avgValue = readings.Average(r => r.Value);

        return new ChartStatsDto(
            MinValue: Math.Round(minReading.Value, 2),
            MinTimestamp: minReading.Timestamp,
            MaxValue: Math.Round(maxReading.Value, 2),
            MaxTimestamp: maxReading.Timestamp,
            AvgValue: Math.Round(avgValue, 2)
        );
    }

    private static TrendDto CalculateTrend(List<Reading> readings, ChartInterval interval)
    {
        if (!readings.Any())
        {
            return new TrendDto(0, 0, "stable");
        }

        var current = readings.Last().Value;

        // Get comparison time based on interval
        var compareTime = interval switch
        {
            ChartInterval.OneHour => DateTime.UtcNow.AddHours(-1),
            ChartInterval.OneDay => DateTime.UtcNow.AddDays(-1),
            ChartInterval.OneWeek => DateTime.UtcNow.AddDays(-7),
            ChartInterval.OneMonth => DateTime.UtcNow.AddMonths(-1),
            _ => DateTime.UtcNow.AddDays(-1)
        };

        // Find the reading closest to the comparison time
        var previousReading = readings
            .Where(r => r.Timestamp <= compareTime)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefault();

        var previous = previousReading?.Value ?? current;

        var change = current - previous;
        var changePercent = previous != 0 ? (change / Math.Abs(previous)) * 100 : 0;

        string direction;
        if (change > 0.1)
            direction = "up";
        else if (change < -0.1)
            direction = "down";
        else
            direction = "stable";

        return new TrendDto(
            Change: Math.Round(change, 2),
            ChangePercent: Math.Round(changePercent, 1),
            Direction: direction
        );
    }

    private static string GetDefaultColor(string measurementType)
    {
        return MeasurementTypeColors.GetValueOrDefault(measurementType.ToLowerInvariant(), "#607D8B");
    }
}
