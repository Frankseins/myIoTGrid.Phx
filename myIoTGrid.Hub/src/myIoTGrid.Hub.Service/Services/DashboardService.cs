using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Dashboard data.
/// Provides location-grouped sensor widgets with sparkline data (Home Assistant style).
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly HubDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<DashboardService> _logger;

    // Default colors for different measurement types
    private static readonly Dictionary<string, string> MeasurementTypeColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = "#FF5722",        // Orange
        ["water_temperature"] = "#00BCD4",  // Cyan
        ["humidity"] = "#2196F3",           // Blue
        ["pressure"] = "#9C27B0",           // Purple
        ["co2"] = "#4CAF50",                // Green
        ["pm25"] = "#795548",               // Brown
        ["pm10"] = "#607D8B",               // Blue-Grey
        ["soil_moisture"] = "#8BC34A",      // Light Green
        ["light"] = "#FFC107",              // Amber
        ["illuminance"] = "#FFC107",        // Amber
        ["uv"] = "#E91E63",                 // Pink
        ["wind_speed"] = "#00BCD4",         // Cyan
        ["rainfall"] = "#3F51B5",           // Indigo
        ["water_level"] = "#009688",        // Teal
        ["battery"] = "#CDDC39",            // Lime
        ["rssi"] = "#9E9E9E"                // Grey
    };

    // Location icons mapping
    private static readonly Dictionary<string, string> LocationIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        ["außen"] = "home",
        ["draußen"] = "home",
        ["garten"] = "yard",
        ["terrasse"] = "deck",
        ["balkon"] = "balcony",
        ["wohnzimmer"] = "weekend",
        ["schlafzimmer"] = "bedroom_parent",
        ["kinderzimmer"] = "bedroom_child",
        ["küche"] = "kitchen",
        ["bad"] = "bathroom",
        ["badezimmer"] = "bathroom",
        ["büro"] = "work",
        ["arbeitszimmer"] = "work",
        ["keller"] = "foundation",
        ["dachboden"] = "roofing",
        ["garage"] = "garage",
        ["flur"] = "meeting_room",
        ["eingang"] = "door_front",
        ["gewächshaus"] = "eco",
        // English
        ["outside"] = "home",
        ["outdoor"] = "home",
        ["garden"] = "yard",
        ["terrace"] = "deck",
        ["balcony"] = "balcony",
        ["living room"] = "weekend",
        ["bedroom"] = "bedroom_parent",
        ["kitchen"] = "kitchen",
        ["bathroom"] = "bathroom",
        ["bath room"] = "bathroom",
        ["office"] = "work",
        ["basement"] = "foundation",
        ["attic"] = "roofing",
        ["garage"] = "garage",
        ["hallway"] = "meeting_room",
        ["entrance"] = "door_front",
        ["greenhouse"] = "eco"
    };

    // Hero locations (displayed as full-width widgets)
    private static readonly HashSet<string> HeroLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        "außen", "outside", "outdoor", "draußen", "garten", "garden"
    };

    // Valid measurement types (filter out sensor model names like dht22, bme280, etc.)
    private static readonly HashSet<string> ValidMeasurementTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "temperature", "water_temperature", "humidity", "pressure", "co2",
        "pm25", "pm10", "soil_moisture", "light", "illuminance", "uv",
        "wind_speed", "rainfall", "water_level", "battery", "rssi",
        "speed", "latitude", "longitude", "altitude"
    };

    public DashboardService(
        HubDbContext context,
        ITenantService tenantService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LocationDashboardDto> GetLocationDashboardAsync(
        SparklinePeriod period = SparklinePeriod.Day,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var (startTime, intervalMinutes) = GetSparklineParameters(period);

        _logger.LogDebug("Getting dashboard data for tenant {TenantId}, period {Period}, startTime {StartTime}",
            tenantId, period, startTime);

        // Get all nodes with their locations and sensor assignments
        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.Location)
            .Include(n => n.SensorAssignments)
                .ThenInclude(a => a.Sensor)
                    .ThenInclude(s => s.Capabilities)
            .Where(n => n.Hub!.TenantId == tenantId)
            .ToListAsync(ct);

        // Get all readings within the time period
        var readings = await _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Timestamp >= startTime)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        // Group readings by NodeId, AssignmentId, and MeasurementType for quick lookup
        // This ensures DHT22 and BME280 readings for same measurement type are kept separate
        var readingsLookup = readings
            .GroupBy(r => (r.NodeId, r.AssignmentId, r.MeasurementType.ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build location groups
        var locationGroups = nodes
            .GroupBy(n => n.Location?.Name ?? "Unbekannt")
            .Select(g => BuildLocationGroup(g.Key, g.ToList(), readingsLookup, intervalMinutes))
            .Where(lg => lg.Widgets.Any()) // Only include locations with widgets
            .OrderByDescending(lg => lg.IsHero)
            .ThenBy(lg => lg.LocationName)
            .ToList();

        return new LocationDashboardDto(locationGroups);
    }

    private LocationGroupDto BuildLocationGroup(
        string locationName,
        List<Node> nodes,
        Dictionary<(Guid NodeId, Guid? AssignmentId, string MeasurementType), List<Reading>> readingsLookup,
        int intervalMinutes)
    {
        var widgets = new List<SensorWidgetDto>();

        foreach (var node in nodes)
        {
            // Get all active sensor assignments for this node
            var activeAssignments = node.SensorAssignments
                .Where(a => a.IsActive)
                .ToList();

            // Process each assignment separately to get separate widgets per sensor
            foreach (var assignment in activeAssignments)
            {
                var capabilities = assignment.Sensor?.Capabilities ?? Enumerable.Empty<SensorCapability>();

                foreach (var capability in capabilities)
                {
                    var measurementType = capability.MeasurementType.ToLowerInvariant();

                    // Skip invalid measurement types
                    if (!ValidMeasurementTypes.Contains(measurementType))
                    {
                        continue;
                    }

                    var key = (node.Id, (Guid?)assignment.Id, measurementType);
                    if (!readingsLookup.TryGetValue(key, out var nodeReadings) || !nodeReadings.Any())
                    {
                        continue; // Skip if no readings for this assignment/measurement type
                    }

                    var widget = BuildSensorWidget(node, assignment, measurementType, nodeReadings, intervalMinutes, locationName);
                    if (widget != null)
                    {
                        widgets.Add(widget);
                    }
                }
            }

            // NOTE: Readings without AssignmentId (legacy) are intentionally excluded
            // Only sensors with active assignments should appear in the dashboard
        }

        return new LocationGroupDto(
            LocationName: locationName,
            LocationIcon: GetLocationIcon(locationName),
            IsHero: IsHeroLocation(locationName),
            Widgets: widgets
        );
    }

    private SensorWidgetDto? BuildSensorWidget(
        Node node,
        NodeSensorAssignment? assignment,
        string measurementType,
        List<Reading> readings,
        int intervalMinutes,
        string locationName)
    {
        if (!readings.Any()) return null;

        var latestReading = readings.MaxBy(r => r.Timestamp)!;

        var capability = assignment?.Sensor?.Capabilities
            .FirstOrDefault(c => c.MeasurementType.Equals(measurementType, StringComparison.OrdinalIgnoreCase));

        // Build sparkline data points (aggregated by interval)
        var dataPoints = BuildSparklineDataPoints(readings, intervalMinutes);

        // Calculate min/max with timestamps
        var minMax = CalculateMinMax(readings);

        // Get color and display info
        var color = assignment?.Sensor?.Color
            ?? GetDefaultColor(measurementType);
        var unit = latestReading.Unit;

        // Get sensor name (e.g., "DHT22", "BME280")
        var sensorName = assignment?.Sensor?.Name ?? "";

        // WidgetId includes AssignmentId to make it unique per sensor
        var widgetId = assignment != null
            ? $"{node.Id}_{assignment.Id}_{measurementType}".ToLowerInvariant()
            : $"{node.Id}_{measurementType}".ToLowerInvariant();

        return new SensorWidgetDto(
            WidgetId: widgetId,
            NodeId: node.Id,
            NodeName: node.Name,
            AssignmentId: assignment?.Id,
            SensorId: assignment?.SensorId,
            MeasurementType: measurementType.ToLowerInvariant(),
            SensorName: sensorName,
            LocationName: locationName,
            Label: sensorName, // Simplified label - just sensor name
            Unit: unit,
            Color: color,
            CurrentValue: latestReading.Value,
            LastUpdate: latestReading.Timestamp,
            MinMax: minMax,
            DataPoints: dataPoints
        );
    }

    private static List<SparklinePointDto> BuildSparklineDataPoints(
        List<Reading> readings,
        int intervalMinutes)
    {
        if (!readings.Any()) return new List<SparklinePointDto>();

        // Group readings into intervals and take average
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
            .Select(g => new SparklinePointDto(
                Timestamp: g.Key,
                Value: Math.Round(g.Average(r => r.Value), 2)
            ))
            .ToList();

        return grouped;
    }

    private static MinMaxDto CalculateMinMax(List<Reading> readings)
    {
        if (!readings.Any())
        {
            return new MinMaxDto(0, DateTime.UtcNow, 0, DateTime.UtcNow);
        }

        var minReading = readings.MinBy(r => r.Value)!;
        var maxReading = readings.MaxBy(r => r.Value)!;

        return new MinMaxDto(
            MinValue: Math.Round(minReading.Value, 2),
            MinTimestamp: minReading.Timestamp,
            MaxValue: Math.Round(maxReading.Value, 2),
            MaxTimestamp: maxReading.Timestamp
        );
    }

    private static (DateTime StartTime, int IntervalMinutes) GetSparklineParameters(SparklinePeriod period)
    {
        return period switch
        {
            SparklinePeriod.Hour => (DateTime.UtcNow.AddHours(-1), 1),      // 1-minute intervals
            SparklinePeriod.Day => (DateTime.UtcNow.AddDays(-1), 15),       // 15-minute intervals
            SparklinePeriod.Week => (DateTime.UtcNow.AddDays(-7), 60),      // 1-hour intervals
            _ => (DateTime.UtcNow.AddDays(-1), 15)
        };
    }

    private static string GetLocationIcon(string locationName)
    {
        return LocationIcons.GetValueOrDefault(locationName.ToLowerInvariant(), "location_on");
    }

    private static bool IsHeroLocation(string locationName)
    {
        return HeroLocations.Contains(locationName);
    }

    private static string GetDefaultColor(string measurementType)
    {
        return MeasurementTypeColors.GetValueOrDefault(measurementType.ToLowerInvariant(), "#607D8B");
    }

    // German display names for measurement types
    private static readonly Dictionary<string, string> MeasurementTypeDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = "Temperatur",
        ["humidity"] = "Luftfeuchtigkeit",
        ["pressure"] = "Luftdruck",
        ["co2"] = "CO₂",
        ["pm25"] = "Feinstaub PM2.5",
        ["pm10"] = "Feinstaub PM10",
        ["soil_moisture"] = "Bodenfeuchtigkeit",
        ["light"] = "Helligkeit",
        ["illuminance"] = "Helligkeit",
        ["uv"] = "UV-Index",
        ["wind_speed"] = "Windgeschwindigkeit",
        ["rainfall"] = "Niederschlag",
        ["water_level"] = "Wasserstand",
        ["water_temperature"] = "Wassertemperatur",
        ["battery"] = "Batterie",
        ["rssi"] = "Signalstärke",
        ["speed"] = "Geschwindigkeit",
        ["latitude"] = "Breitengrad",
        ["longitude"] = "Längengrad",
        ["altitude"] = "Höhe"
    };

    private static string FormatMeasurementType(string measurementType)
    {
        // Try German display name first, then fallback to Title Case
        if (MeasurementTypeDisplayNames.TryGetValue(measurementType, out var displayName))
        {
            return displayName;
        }

        // Convert snake_case to Title Case as fallback
        return string.Join(" ",
            measurementType.Split('_')
                .Select(word => char.ToUpper(word[0]) + word[1..]));
    }

    /// <inheritdoc />
    public async Task<LocationDashboardDto> GetFilteredDashboardAsync(
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var (startTime, intervalMinutes) = GetSparklineParameters(filter.Period);

        _logger.LogDebug("Getting filtered dashboard for tenant {TenantId}, filter: locations={Locations}, types={Types}",
            tenantId, filter.Locations?.Length ?? 0, filter.MeasurementTypes?.Length ?? 0);

        // Get all nodes with their locations and sensor assignments
        var nodesQuery = _context.Nodes
            .AsNoTracking()
            .Include(n => n.Location)
            .Include(n => n.SensorAssignments)
                .ThenInclude(a => a.Sensor)
                    .ThenInclude(s => s.Capabilities)
            .Where(n => n.Hub!.TenantId == tenantId);

        // Filter by locations if specified
        if (filter.Locations?.Length > 0)
        {
            nodesQuery = nodesQuery.Where(n =>
                n.Location != null && filter.Locations.Contains(n.Location.Name));
        }

        var nodes = await nodesQuery.ToListAsync(ct);

        // Get all readings within the time period
        var readingsQuery = _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Timestamp >= startTime);

        // Filter by measurement types if specified
        if (filter.MeasurementTypes?.Length > 0)
        {
            var lowerTypes = filter.MeasurementTypes.Select(t => t.ToLowerInvariant()).ToArray();
            readingsQuery = readingsQuery.Where(r => lowerTypes.Contains(r.MeasurementType.ToLower()));
        }

        var readings = await readingsQuery.OrderBy(r => r.Timestamp).ToListAsync(ct);

        // Group readings by NodeId, AssignmentId, and MeasurementType for quick lookup
        // This ensures DHT22 and BME280 readings for same measurement type are kept separate
        var readingsLookup = readings
            .GroupBy(r => (r.NodeId, r.AssignmentId, r.MeasurementType.ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build location groups
        var locationGroups = nodes
            .GroupBy(n => n.Location?.Name ?? "Unbekannt")
            .Select(g => BuildLocationGroup(g.Key, g.ToList(), readingsLookup, intervalMinutes, filter.MeasurementTypes))
            .Where(lg => lg.Widgets.Any())
            .OrderByDescending(lg => lg.IsHero)
            .ThenBy(lg => lg.LocationName)
            .ToList();

        return new LocationDashboardDto(locationGroups);
    }

    private LocationGroupDto BuildLocationGroup(
        string locationName,
        List<Node> nodes,
        Dictionary<(Guid NodeId, Guid? AssignmentId, string MeasurementType), List<Reading>> readingsLookup,
        int intervalMinutes,
        string[]? measurementTypeFilter)
    {
        var widgets = new List<SensorWidgetDto>();

        foreach (var node in nodes)
        {
            // Get all active sensor assignments for this node
            var activeAssignments = node.SensorAssignments
                .Where(a => a.IsActive)
                .ToList();

            // Process each assignment separately to get separate widgets per sensor
            foreach (var assignment in activeAssignments)
            {
                var capabilities = assignment.Sensor?.Capabilities ?? Enumerable.Empty<SensorCapability>();

                foreach (var capability in capabilities)
                {
                    var measurementType = capability.MeasurementType.ToLowerInvariant();

                    // Skip invalid measurement types
                    if (!ValidMeasurementTypes.Contains(measurementType))
                    {
                        continue;
                    }

                    // Apply measurement type filter if specified
                    if (measurementTypeFilter?.Length > 0)
                    {
                        var lowerFilter = measurementTypeFilter.Select(t => t.ToLowerInvariant()).ToArray();
                        if (!lowerFilter.Contains(measurementType))
                        {
                            continue;
                        }
                    }

                    var key = (node.Id, (Guid?)assignment.Id, measurementType);
                    if (!readingsLookup.TryGetValue(key, out var nodeReadings) || !nodeReadings.Any())
                    {
                        continue; // Skip if no readings for this assignment/measurement type
                    }

                    var widget = BuildSensorWidget(node, assignment, measurementType, nodeReadings, intervalMinutes, locationName);
                    if (widget != null)
                    {
                        widgets.Add(widget);
                    }
                }
            }

            // NOTE: Readings without AssignmentId (legacy) are intentionally excluded
            // Only sensors with active assignments should appear in the dashboard
        }

        return new LocationGroupDto(
            LocationName: locationName,
            LocationIcon: GetLocationIcon(locationName),
            IsHero: IsHeroLocation(locationName),
            Widgets: widgets
        );
    }

    /// <inheritdoc />
    public async Task<DashboardFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get all unique locations (filter out nulls)
        var locations = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.Location)
            .Where(n => n.Hub!.TenantId == tenantId && n.Location != null && n.Location.Name != null)
            .Select(n => n.Location!.Name!)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(ct);

        // Get all unique measurement types from readings (only valid ones)
        var measurementTypes = await _context.Readings
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.MeasurementType)
            .Distinct()
            .ToListAsync(ct);

        // Filter to valid measurement types and get German display names
        var validTypes = measurementTypes
            .Where(mt => ValidMeasurementTypes.Contains(mt))
            .OrderBy(mt => FormatMeasurementType(mt))
            .ToList();

        return new DashboardFilterOptionsDto(locations, validTypes);
    }
}
