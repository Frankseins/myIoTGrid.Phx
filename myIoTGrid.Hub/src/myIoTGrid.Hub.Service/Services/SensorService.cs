using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Sensor management (v3.0).
/// Complete sensor definition with hardware configuration and calibration.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public class SensorService : ISensorService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SensorService> _logger;

    private const string CacheKeyPrefix = "Sensors_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public SensorService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<SensorService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var sensors = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.Capabilities)
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<SensorDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Sensors
            .AsNoTracking()
            .Include(s => s.Capabilities)
            .Where(s => s.TenantId == tenantId);

        // Global search
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.Code.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)) ||
                (s.SerialNumber != null && s.SerialNumber.ToLower().Contains(term)) ||
                (s.Manufacturer != null && s.Manufacturer.ToLower().Contains(term)) ||
                s.Category.ToLower().Contains(term));
        }

        // Filter by category
        if (queryParams.Filters?.TryGetValue("category", out var category) == true
            && !string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category.ToLower());
        }

        // Filter by protocol
        if (queryParams.Filters?.TryGetValue("protocol", out var protocol) == true
            && Enum.TryParse<CommunicationProtocol>(protocol, true, out var protocolEnum))
        {
            query = query.Where(s => s.Protocol == protocolEnum);
        }

        // Filter by manufacturer
        if (queryParams.Filters?.TryGetValue("manufacturer", out var manufacturer) == true
            && !string.IsNullOrWhiteSpace(manufacturer))
        {
            var manufacturerLower = manufacturer.ToLower();
            query = query.Where(s => s.Manufacturer != null && s.Manufacturer.ToLower().Contains(manufacturerLower));
        }

        // Filter by isActive
        if (queryParams.Filters?.TryGetValue("isActive", out var isActiveStr) == true
            && bool.TryParse(isActiveStr, out var isActive))
        {
            query = query.Where(s => s.IsActive == isActive);
        }

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Sorting
        query = query.ApplySort(queryParams, "Name");

        // Paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);
        var dtos = items.ToDtos();

        return PagedResultDto<SensorDto>.Create(dtos, totalRecords, queryParams);
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var normalizedCode = code.ToLowerInvariant();

        var sensor = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Code == normalizedCode, ct);

        return sensor?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var normalizedCategory = category.ToLowerInvariant();

        var sensors = await _context.Sensors
            .AsNoTracking()
            .Include(s => s.Capabilities)
            .Where(s => s.TenantId == tenantId && s.Category == normalizedCategory && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sensors.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorCapabilityDto>> GetCapabilitiesAsync(Guid sensorId, CancellationToken ct = default)
    {
        var capabilities = await _context.SensorCapabilities
            .AsNoTracking()
            .Where(c => c.SensorId == sensorId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return capabilities.ToDtos();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CreateAsync(CreateSensorDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var normalizedCode = dto.Code.ToLowerInvariant();

        // Check if Code already exists for this tenant
        var exists = await _context.Sensors
            .AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.Code == normalizedCode, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Sensor with Code '{normalizedCode}' already exists.");
        }

        var sensor = dto.ToEntity(tenantId);

        _context.Sensors.Add(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with capabilities
        sensor = await _context.Sensors
            .Include(s => s.Capabilities)
            .FirstAsync(s => s.Id == sensor.Id, ct);

        InvalidateCache(tenantId);

        _logger.LogInformation("Sensor created: {Code} ({Name})", sensor.Code, sensor.Name);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> UpdateAsync(Guid id, UpdateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        // Identify capabilities to delete BEFORE ApplyUpdate modifies the collection
        var capabilitiesToDelete = new List<SensorCapability>();
        if (dto.Capabilities != null)
        {
            var updatedIds = dto.Capabilities
                .Where(c => c.Id.HasValue)
                .Select(c => c.Id!.Value)
                .ToHashSet();

            // Get the actual tracked entities that will be removed
            capabilitiesToDelete = sensor.Capabilities
                .Where(c => !updatedIds.Contains(c.Id))
                .ToList();
        }

        // Get IDs of existing capabilities BEFORE ApplyUpdate
        var existingCapabilityIds = sensor.Capabilities.Select(c => c.Id).ToHashSet();

        // Apply updates (adds new and updates existing capabilities, but does NOT remove)
        sensor.ApplyUpdate(dto);

        // Identify and explicitly mark NEW capabilities as Added
        // (EF Core might not auto-detect them as new if they have an explicit Id)
        foreach (var cap in sensor.Capabilities)
        {
            if (!existingCapabilityIds.Contains(cap.Id))
            {
                _context.Entry(cap).State = EntityState.Added;
                _logger.LogDebug("Marking capability {CapabilityId} ({Type}) as Added for Sensor {SensorId}",
                    cap.Id, cap.MeasurementType, id);
            }
        }

        // Now handle deletion: Remove from collection AND mark for deletion
        // Both steps are needed for EF Core to properly track the deletion
        if (capabilitiesToDelete.Count > 0)
        {
            foreach (var cap in capabilitiesToDelete)
            {
                // Remove from the in-memory collection
                sensor.Capabilities.Remove(cap);
                // Explicitly mark for deletion in the change tracker
                _context.SensorCapabilities.Remove(cap);
            }
            _logger.LogInformation("Removing {Count} capabilities from Sensor {SensorId}",
                capabilitiesToDelete.Count, id);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        InvalidateCache(sensor.TenantId);

        _logger.LogInformation("Sensor updated: {SensorId}", id);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorDto> CalibrateAsync(Guid id, CalibrateSensorDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .Include(s => s.Capabilities)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        sensor.ApplyCalibration(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        InvalidateCache(sensor.TenantId);

        _logger.LogInformation("Sensor calibrated: {SensorId} (Offset: {Offset}, Gain: {Gain})",
            id, dto.OffsetCorrection, dto.GainCorrection);

        return sensor.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sensor == null)
        {
            throw new InvalidOperationException($"Sensor with Id '{id}' not found.");
        }

        // Check if sensor has assignments
        var hasAssignments = await _context.NodeSensorAssignments
            .AnyAsync(a => a.SensorId == id, ct);

        if (hasAssignments)
        {
            throw new InvalidOperationException("Cannot delete sensor with active assignments. Remove assignments first.");
        }

        var tenantId = sensor.TenantId;
        _context.Sensors.Remove(sensor);
        await _unitOfWork.SaveChangesAsync(ct);

        InvalidateCache(tenantId);

        _logger.LogInformation("Sensor deleted: {SensorId}", id);
    }

    /// <inheritdoc />
    public async Task SeedDefaultSensorsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Check if any sensors already exist for this tenant
        var existingCodes = await _context.Sensors
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.Code)
            .ToListAsync(ct);

        var newSensors = new List<Sensor>();
        var now = DateTime.UtcNow;

        // DHT22 - Temperature & Humidity
        if (!existingCodes.Contains("dht22"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "dht22",
                name: "DHT22 (AM2302)",
                manufacturer: "Aosong",
                protocol: CommunicationProtocol.Digital,
                category: "climate",
                icon: "thermostat",
                color: "#FF5722",
                digitalPin: 4,
                intervalSeconds: 2,
                minIntervalSeconds: 2,
                capabilities: new[]
                {
                    CreateCapability("temperature", "Temperatur", "°C", -40, 80, 0.1, 0.5, 1026, "TemperatureMeasurement"),
                    CreateCapability("humidity", "Luftfeuchtigkeit", "%", 0, 100, 1, 2, 1029, "RelativeHumidityMeasurement")
                },
                now: now
            ));
        }

        // BME280 - Temperature, Humidity & Pressure
        if (!existingCodes.Contains("bme280"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "bme280",
                name: "GY-BME280 Breakout (I²C)",
                manufacturer: "Bosch",
                protocol: CommunicationProtocol.I2C,
                category: "climate",
                icon: "cloud",
                color: "#2196F3",
                i2CAddress: "0x76",
                sdaPin: 21,
                sclPin: 22,
                intervalSeconds: 60,
                capabilities: new[]
                {
                    CreateCapability("temperature", "Temperatur", "°C", -40, 85, 0.01, 0.5, 1026, "TemperatureMeasurement"),
                    CreateCapability("humidity", "Luftfeuchtigkeit", "%", 0, 100, 0.008, 3, 1029, "RelativeHumidityMeasurement"),
                    CreateCapability("pressure", "Luftdruck", "hPa", 300, 1100, 0.18, 1, 1027, "PressureMeasurement")
                },
                now: now
            ));
        }

        // BH1750 - Light
        if (!existingCodes.Contains("bh1750"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "bh1750",
                name: "BH1750 Lichtsensor (I²C)",
                manufacturer: "ROHM",
                protocol: CommunicationProtocol.I2C,
                category: "climate",
                icon: "light_mode",
                color: "#FFC107",
                i2CAddress: "0x23",
                sdaPin: 21,
                sclPin: 22,
                intervalSeconds: 60,
                capabilities: new[]
                {
                    CreateCapability("illuminance", "Helligkeit", "lux", 1, 65535, 1, 20, 1024, "IlluminanceMeasurement")
                },
                now: now
            ));
        }

        // GY-302 - Light (BH1750 Breakout Board)
        if (!existingCodes.Contains("gy302"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "gy302",
                name: "GY-302 Lichstärkensensor",
                manufacturer: "Generic",
                protocol: CommunicationProtocol.I2C,
                category: "climate",
                icon: "light_mode",
                color: "#FFEB3B",
                i2CAddress: "0x23",
                sdaPin: 21,
                sclPin: 22,
                intervalSeconds: 60,
                capabilities: new[]
                {
                    CreateCapability("illuminance", "Helligkeit", "lux", 1, 65535, 1, 20, 1024, "IlluminanceMeasurement")
                },
                now: now
            ));
        }

        // DS18B20 - Water Temperature
        if (!existingCodes.Contains("ds18b20"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "ds18b20",
                name: "DS18B20 wasserdicht",
                manufacturer: "Maxim",
                protocol: CommunicationProtocol.OneWire,
                category: "water",
                icon: "water",
                color: "#00BCD4",
                oneWirePin: 4,
                intervalSeconds: 60,
                capabilities: new[]
                {
                    CreateCapability("water_temperature", "Wassertemperatur", "°C", -55, 125, 0.0625, 0.5, 64513)
                },
                now: now
            ));
        }

        // JSN-SR04T - Water Level
        if (!existingCodes.Contains("jsn-sr04t"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "jsn-sr04t",
                name: "JSN-SR04T Ultraschall wasserdicht",
                protocol: CommunicationProtocol.UltraSonic,
                category: "water",
                icon: "waves",
                color: "#03A9F4",
                triggerPin: 5,
                echoPin: 18,
                intervalSeconds: 30,
                capabilities: new[]
                {
                    CreateCapability("water_level", "Wasserstand", "cm", 25, 450, 1, 1, 64512)
                },
                now: now
            ));
        }

        // NEO-6M - GPS (UART: RX=GPIO16, TX=GPIO17 - ESP32 Serial2 defaults)
        if (!existingCodes.Contains("neo-6m"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "neo-6m",
                name: "GPS-Modul NEO-6M mit Antenne",
                manufacturer: "u-blox",
                protocol: CommunicationProtocol.UART,
                category: "location",
                icon: "location_on",
                color: "#4CAF50",
                analogPin: 16,      // RX Pin (ESP32 RX ← GPS TX)
                digitalPin: 17,     // TX Pin (ESP32 TX → GPS RX)
                intervalSeconds: 1,
                capabilities: new[]
                {
                    CreateCapability("latitude", "Breitengrad", "°", -90, 90, 0.000001, 2.5),
                    CreateCapability("longitude", "Längengrad", "°", -180, 180, 0.000001, 2.5),
                    CreateCapability("altitude", "Höhe", "m", -500, 50000, 0.1, 10),
                    CreateCapability("speed", "Geschwindigkeit", "km/h", 0, 500, 0.1, 0.5),
                    CreateCapability("gps_satellites", "Satelliten", "", 0, 50, 1, 0),
                    CreateCapability("gps_fix", "Fix-Status", "", 0, 3, 1, 0),
                    CreateCapability("gps_hdop", "HDOP", "", 0, 100, 0.01, 0.1)
                },
                now: now
            ));
        }

        // SR04M-2 - Waterproof Ultrasonic Distance Sensor (UART Mode)
        if (!existingCodes.Contains("sr04m-2"))
        {
            newSensors.Add(CreateSensor(
                tenantId: tenantId,
                code: "sr04m-2",
                name: "SR04M-2 Ultraschall wasserdicht (UART)",
                manufacturer: "Generic",
                protocol: CommunicationProtocol.UART,
                category: "water",
                icon: "straighten",
                color: "#009688",
                analogPin: 19,      // RX Pin (ESP32 RX ← Sensor TX)
                digitalPin: 18,     // TX Pin (ESP32 TX → Sensor RX)
                intervalSeconds: 5,
                minIntervalSeconds: 1,
                capabilities: new[]
                {
                    CreateCapability("distance", "Distanz", "cm", 20, 600, 1, 1, 64514),
                    CreateCapability("water_level", "Wasserstand", "cm", 20, 600, 1, 1, 64512)
                },
                now: now
            ));
        }

        if (newSensors.Count > 0)
        {
            await _context.Sensors.AddRangeAsync(newSensors, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            InvalidateCache(tenantId);

            _logger.LogInformation("{Count} default Sensors created", newSensors.Count);
        }

        // Add missing capabilities to existing sensors
        await AddMissingCapabilitiesToExistingSensorsAsync(tenantId, ct);
    }

    /// <summary>
    /// Adds missing capabilities to existing sensors based on the expected seed definitions.
    /// This ensures that sensors created before new capabilities were added get updated.
    /// </summary>
    private async Task AddMissingCapabilitiesToExistingSensorsAsync(Guid tenantId, CancellationToken ct)
    {
        // Define expected capabilities per sensor code
        var expectedCapabilities = new Dictionary<string, SensorCapability[]>
        {
            ["neo-6m"] = new[]
            {
                CreateCapability("latitude", "Breitengrad", "°", -90, 90, 0.000001, 2.5),
                CreateCapability("longitude", "Längengrad", "°", -180, 180, 0.000001, 2.5),
                CreateCapability("altitude", "Höhe", "m", -500, 50000, 0.1, 10),
                CreateCapability("speed", "Geschwindigkeit", "km/h", 0, 500, 0.1, 0.5),
                CreateCapability("gps_satellites", "Satelliten", "", 0, 50, 1, 0),
                CreateCapability("gps_fix", "Fix-Status", "", 0, 3, 1, 0),
                CreateCapability("gps_hdop", "HDOP", "", 0, 100, 0.01, 0.1)
            }
        };

        // Get existing sensors with their capabilities
        var existingSensors = await _context.Sensors
            .Include(s => s.Capabilities)
            .Where(s => s.TenantId == tenantId && expectedCapabilities.Keys.Contains(s.Code))
            .ToListAsync(ct);

        var capabilitiesAdded = 0;

        foreach (var sensor in existingSensors)
        {
            if (!expectedCapabilities.TryGetValue(sensor.Code, out var expected))
                continue;

            var existingMeasurementTypes = sensor.Capabilities
                .Select(c => c.MeasurementType)
                .ToHashSet();

            foreach (var expectedCap in expected)
            {
                if (!existingMeasurementTypes.Contains(expectedCap.MeasurementType))
                {
                    // Add the missing capability
                    var newCap = CreateCapability(
                        expectedCap.MeasurementType,
                        expectedCap.DisplayName,
                        expectedCap.Unit,
                        expectedCap.MinValue,
                        expectedCap.MaxValue,
                        expectedCap.Resolution,
                        expectedCap.Accuracy,
                        expectedCap.MatterClusterId,
                        expectedCap.MatterClusterName
                    );
                    newCap.SensorId = sensor.Id;
                    sensor.Capabilities.Add(newCap);
                    capabilitiesAdded++;

                    _logger.LogInformation(
                        "Added missing capability '{Capability}' to sensor '{SensorCode}'",
                        expectedCap.MeasurementType, sensor.Code);
                }
            }
        }

        if (capabilitiesAdded > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
            InvalidateCache(tenantId);
            _logger.LogInformation("{Count} missing capabilities added to existing sensors", capabilitiesAdded);
        }
    }

    private void InvalidateCache(Guid tenantId)
    {
        _cache.Remove($"{CacheKeyPrefix}{tenantId}");
    }

    private static Sensor CreateSensor(
        Guid tenantId,
        string code,
        string name,
        CommunicationProtocol protocol,
        string category,
        string icon,
        string color,
        DateTime now,
        string? manufacturer = null,
        string? i2CAddress = null,
        int? sdaPin = null,
        int? sclPin = null,
        int? oneWirePin = null,
        int? analogPin = null,
        int? digitalPin = null,
        int? triggerPin = null,
        int? echoPin = null,
        int intervalSeconds = 60,
        int minIntervalSeconds = 1,
        SensorCapability[]? capabilities = null)
    {
        var sensorId = Guid.NewGuid();
        var sensor = new Sensor
        {
            Id = sensorId,
            TenantId = tenantId,
            Code = code,
            Name = name,
            Manufacturer = manufacturer,
            Protocol = protocol,
            I2CAddress = i2CAddress,
            SdaPin = sdaPin,
            SclPin = sclPin,
            OneWirePin = oneWirePin,
            AnalogPin = analogPin,
            DigitalPin = digitalPin,
            TriggerPin = triggerPin,
            EchoPin = echoPin,
            IntervalSeconds = intervalSeconds,
            MinIntervalSeconds = minIntervalSeconds,
            Category = category,
            Icon = icon,
            Color = color,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (capabilities != null)
        {
            foreach (var cap in capabilities)
            {
                cap.SensorId = sensorId;
                sensor.Capabilities.Add(cap);
            }
        }

        return sensor;
    }

    private static SensorCapability CreateCapability(
        string measurementType,
        string displayName,
        string unit,
        double? minValue,
        double? maxValue,
        double resolution,
        double accuracy,
        uint? matterClusterId = null,
        string? matterClusterName = null)
    {
        return new SensorCapability
        {
            Id = Guid.NewGuid(),
            MeasurementType = measurementType,
            DisplayName = displayName,
            Unit = unit,
            MinValue = minValue,
            MaxValue = maxValue,
            Resolution = resolution,
            Accuracy = accuracy,
            MatterClusterId = matterClusterId,
            MatterClusterName = matterClusterName,
            IsActive = true
        };
    }
}
