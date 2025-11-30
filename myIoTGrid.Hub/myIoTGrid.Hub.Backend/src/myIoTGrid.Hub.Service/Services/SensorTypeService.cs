using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;
using myIoTGrid.Hub.Shared.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for SensorType management.
/// Hardware sensor library with default configurations.
/// </summary>
public class SensorTypeService : ISensorTypeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SensorTypeService> _logger;

    private const string CacheKey = "SensorTypes_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public SensorTypeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<SensorTypeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .Where(st => st.IsActive)
            .OrderBy(st => st.Category)
            .ThenBy(st => st.Name)
            .ToListAsync(ct);

        return sensorTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<SensorTypeDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var query = _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .Where(st => st.IsActive);

        // Global search
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.ToLower();
            query = query.Where(st =>
                st.Name.ToLower().Contains(term) ||
                st.Code.ToLower().Contains(term) ||
                (st.Manufacturer != null && st.Manufacturer.ToLower().Contains(term)) ||
                st.Category.ToLower().Contains(term));
        }

        // Filter by category
        if (queryParams.Filters?.TryGetValue("category", out var category) == true
            && !string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(st => st.Category == category);
        }

        // Filter by protocol
        if (queryParams.Filters?.TryGetValue("protocol", out var protocol) == true
            && Enum.TryParse<CommunicationProtocol>(protocol, true, out var protocolEnum))
        {
            query = query.Where(st => st.Protocol == protocolEnum);
        }

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Sorting
        query = query.ApplySort(queryParams, "Name");

        // Paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);
        var dtos = items.ToDtos();

        return PagedResultDto<SensorTypeDto>.Create(dtos, totalRecords, queryParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetAllCachedAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IEnumerable<SensorTypeDto>? cached) && cached != null)
        {
            return cached;
        }

        var sensorTypes = await GetAllAsync(ct);
        var sensorTypeList = sensorTypes.ToList();

        _cache.Set(CacheKey, sensorTypeList, CacheDuration);

        return sensorTypeList;
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .FirstOrDefaultAsync(st => st.Id == id, ct);

        return sensorType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.ToLowerInvariant();

        var sensorType = await _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .FirstOrDefaultAsync(st => st.Code == normalizedCode, ct);

        return sensorType?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var normalizedCategory = category.ToLowerInvariant();

        var sensorTypes = await _context.SensorTypes
            .AsNoTracking()
            .Include(st => st.Capabilities)
            .Where(st => st.Category == normalizedCategory && st.IsActive)
            .OrderBy(st => st.Name)
            .ToListAsync(ct);

        return sensorTypes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SensorTypeCapabilityDto>> GetCapabilitiesAsync(Guid sensorTypeId, CancellationToken ct = default)
    {
        var capabilities = await _context.SensorTypeCapabilities
            .AsNoTracking()
            .Where(c => c.SensorTypeId == sensorTypeId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return capabilities.Select(c => c.ToDto());
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto> CreateAsync(CreateSensorTypeDto dto, CancellationToken ct = default)
    {
        var normalizedCode = dto.Code.ToLowerInvariant();

        // Check if Code already exists
        var exists = await _context.SensorTypes
            .AsNoTracking()
            .AnyAsync(st => st.Code == normalizedCode, ct);

        if (exists)
        {
            throw new InvalidOperationException($"SensorType with Code '{normalizedCode}' already exists.");
        }

        var sensorType = dto.ToEntity();

        _context.SensorTypes.Add(sensorType);
        await _unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        _cache.Remove(CacheKey);

        _logger.LogInformation("SensorType created: {Code} ({Name})", sensorType.Code, sensorType.Name);

        return sensorType.ToDto();
    }

    /// <inheritdoc />
    public async Task<SensorTypeDto> UpdateAsync(Guid id, UpdateSensorTypeDto dto, CancellationToken ct = default)
    {
        var sensorType = await _context.SensorTypes
            .Include(st => st.Capabilities)
            .FirstOrDefaultAsync(st => st.Id == id, ct);

        if (sensorType == null)
        {
            throw new InvalidOperationException($"SensorType with Id '{id}' not found.");
        }

        // Apply updates
        if (!string.IsNullOrEmpty(dto.Name))
            sensorType.Name = dto.Name;

        if (dto.Manufacturer != null)
            sensorType.Manufacturer = dto.Manufacturer;

        if (dto.DatasheetUrl != null)
            sensorType.DatasheetUrl = dto.DatasheetUrl;

        if (dto.Description != null)
            sensorType.Description = dto.Description;

        if (dto.DefaultI2CAddress != null)
            sensorType.DefaultI2CAddress = dto.DefaultI2CAddress;

        if (dto.DefaultSdaPin.HasValue)
            sensorType.DefaultSdaPin = dto.DefaultSdaPin;

        if (dto.DefaultSclPin.HasValue)
            sensorType.DefaultSclPin = dto.DefaultSclPin;

        if (dto.DefaultOneWirePin.HasValue)
            sensorType.DefaultOneWirePin = dto.DefaultOneWirePin;

        if (dto.DefaultAnalogPin.HasValue)
            sensorType.DefaultAnalogPin = dto.DefaultAnalogPin;

        if (dto.DefaultDigitalPin.HasValue)
            sensorType.DefaultDigitalPin = dto.DefaultDigitalPin;

        if (dto.DefaultTriggerPin.HasValue)
            sensorType.DefaultTriggerPin = dto.DefaultTriggerPin;

        if (dto.DefaultEchoPin.HasValue)
            sensorType.DefaultEchoPin = dto.DefaultEchoPin;

        if (dto.DefaultIntervalSeconds.HasValue)
            sensorType.DefaultIntervalSeconds = dto.DefaultIntervalSeconds.Value;

        if (dto.MinIntervalSeconds.HasValue)
            sensorType.MinIntervalSeconds = dto.MinIntervalSeconds.Value;

        if (dto.WarmupTimeMs.HasValue)
            sensorType.WarmupTimeMs = dto.WarmupTimeMs.Value;

        if (dto.DefaultOffsetCorrection.HasValue)
            sensorType.DefaultOffsetCorrection = dto.DefaultOffsetCorrection.Value;

        if (dto.DefaultGainCorrection.HasValue)
            sensorType.DefaultGainCorrection = dto.DefaultGainCorrection.Value;

        if (dto.Category != null)
            sensorType.Category = dto.Category;

        if (dto.Icon != null)
            sensorType.Icon = dto.Icon;

        if (dto.Color != null)
            sensorType.Color = dto.Color;

        if (dto.IsActive.HasValue)
            sensorType.IsActive = dto.IsActive.Value;

        sensorType.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        _cache.Remove(CacheKey);

        return sensorType.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sensorType = await _context.SensorTypes
            .FirstOrDefaultAsync(st => st.Id == id, ct);

        if (sensorType == null)
        {
            throw new InvalidOperationException($"SensorType with Id '{id}' not found.");
        }

        if (sensorType.IsGlobal)
        {
            throw new InvalidOperationException("Cannot delete global SensorType.");
        }

        _context.SensorTypes.Remove(sensorType);
        await _unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        _cache.Remove(CacheKey);

        _logger.LogInformation("SensorType deleted: {Code}", sensorType.Code);
    }

    /// <inheritdoc />
    public async Task SyncFromCloudAsync(CancellationToken ct = default)
    {
        // Placeholder for Cloud synchronization
        _logger.LogInformation("Cloud-Sync for SensorTypes is not yet implemented");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SeedDefaultTypesAsync(CancellationToken ct = default)
    {
        var existingCodes = await _context.SensorTypes
            .AsNoTracking()
            .Select(st => st.Code)
            .ToListAsync(ct);

        var newTypes = new List<SensorType>();
        var now = DateTime.UtcNow;

        // DHT22 - Temperature & Humidity
        if (!existingCodes.Contains("dht22"))
        {
            newTypes.Add(CreateSensorType(
                code: "dht22",
                name: "DHT22 (AM2302)",
                manufacturer: "Aosong",
                protocol: CommunicationProtocol.Digital,
                category: "climate",
                icon: "thermostat",
                color: "#FF5722",
                defaultDigitalPin: 4,
                defaultIntervalSeconds: 2,
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
            newTypes.Add(CreateSensorType(
                code: "bme280",
                name: "GY-BME280 Breakout (I²C)",
                manufacturer: "Bosch",
                protocol: CommunicationProtocol.I2C,
                category: "climate",
                icon: "cloud",
                color: "#2196F3",
                defaultI2CAddress: "0x76",
                defaultSdaPin: 21,
                defaultSclPin: 22,
                defaultIntervalSeconds: 60,
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
            newTypes.Add(CreateSensorType(
                code: "bh1750",
                name: "BH1750 Lichtsensor (I²C)",
                manufacturer: "ROHM",
                protocol: CommunicationProtocol.I2C,
                category: "climate",
                icon: "light_mode",
                color: "#FFC107",
                defaultI2CAddress: "0x23",
                defaultSdaPin: 21,
                defaultSclPin: 22,
                defaultIntervalSeconds: 60,
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
            newTypes.Add(CreateSensorType(
                code: "ds18b20",
                name: "DS18B20 wasserdicht",
                manufacturer: "Maxim",
                protocol: CommunicationProtocol.OneWire,
                category: "water",
                icon: "water",
                color: "#00BCD4",
                defaultOneWirePin: 4,
                defaultIntervalSeconds: 60,
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
            newTypes.Add(CreateSensorType(
                code: "jsn-sr04t",
                name: "JSN-SR04T Ultraschall wasserdicht",
                protocol: CommunicationProtocol.UltraSonic,
                category: "water",
                icon: "waves",
                color: "#03A9F4",
                defaultTriggerPin: 5,
                defaultEchoPin: 18,
                defaultIntervalSeconds: 30,
                capabilities: new[]
                {
                    CreateCapability("water_level", "Wasserstand", "cm", 25, 450, 1, 1, 64512)
                },
                now: now
            ));
        }

        // NEO-6M - GPS
        if (!existingCodes.Contains("neo-6m"))
        {
            newTypes.Add(CreateSensorType(
                code: "neo-6m",
                name: "GPS-Modul NEO-6M mit Antenne",
                manufacturer: "u-blox",
                protocol: CommunicationProtocol.UART,
                category: "location",
                icon: "location_on",
                color: "#4CAF50",
                defaultIntervalSeconds: 1,
                capabilities: new[]
                {
                    CreateCapability("latitude", "Breitengrad", "°", -90, 90, 0.000001, 2.5),
                    CreateCapability("longitude", "Längengrad", "°", -180, 180, 0.000001, 2.5),
                    CreateCapability("altitude", "Höhe", "m", -500, 50000, 0.1, 10),
                    CreateCapability("speed", "Geschwindigkeit", "km/h", 0, 500, 0.1, 0.5)
                },
                now: now
            ));
        }

        if (newTypes.Count > 0)
        {
            await _context.SensorTypes.AddRangeAsync(newTypes, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Invalidate cache
            _cache.Remove(CacheKey);

            _logger.LogInformation("{Count} default SensorTypes created", newTypes.Count);
        }
    }

    private static SensorType CreateSensorType(
        string code,
        string name,
        CommunicationProtocol protocol,
        string category,
        string icon,
        string color,
        DateTime now,
        string? manufacturer = null,
        string? defaultI2CAddress = null,
        int? defaultSdaPin = null,
        int? defaultSclPin = null,
        int? defaultOneWirePin = null,
        int? defaultAnalogPin = null,
        int? defaultDigitalPin = null,
        int? defaultTriggerPin = null,
        int? defaultEchoPin = null,
        int defaultIntervalSeconds = 60,
        int minIntervalSeconds = 1,
        SensorTypeCapability[]? capabilities = null)
    {
        var sensorType = new SensorType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Manufacturer = manufacturer,
            Protocol = protocol,
            DefaultI2CAddress = defaultI2CAddress,
            DefaultSdaPin = defaultSdaPin,
            DefaultSclPin = defaultSclPin,
            DefaultOneWirePin = defaultOneWirePin,
            DefaultAnalogPin = defaultAnalogPin,
            DefaultDigitalPin = defaultDigitalPin,
            DefaultTriggerPin = defaultTriggerPin,
            DefaultEchoPin = defaultEchoPin,
            DefaultIntervalSeconds = defaultIntervalSeconds,
            MinIntervalSeconds = minIntervalSeconds,
            Category = category,
            Icon = icon,
            Color = color,
            IsGlobal = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (capabilities != null)
        {
            foreach (var cap in capabilities)
            {
                cap.SensorTypeId = sensorType.Id;
                sensorType.Capabilities.Add(cap);
            }
        }

        return sensorType;
    }

    private static SensorTypeCapability CreateCapability(
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
        return new SensorTypeCapability
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
