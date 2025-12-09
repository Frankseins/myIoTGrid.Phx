using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Helpers;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Node (ESP32/LoRa32 Device) management.
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
public class NodeService : INodeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<NodeService> _logger;

    public NodeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ISignalRNotificationService signalRNotificationService,
        ILogger<NodeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

        return nodes.Select(n => n.ToDto());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default)
    {
        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .Where(n => n.HubId == hubId)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

        return nodes.Select(n => n.ToDto());
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<NodeDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var query = _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .Include(n => n.Hub)
            .AsQueryable();

        // Global search (Name, NodeId)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.ApplySearch(
                queryParams.Search,
                n => n.Name,
                n => n.NodeId);
        }

        // Filter by HubId
        if (queryParams.Filters?.TryGetValue("hubId", out var hubIdFilter) == true &&
            Guid.TryParse(hubIdFilter, out var hubId))
        {
            query = query.Where(n => n.HubId == hubId);
        }

        // Filter by Protocol
        if (queryParams.Filters?.TryGetValue("protocol", out var protocolFilter) == true &&
            Enum.TryParse<Protocol>(protocolFilter, true, out var protocol))
        {
            query = query.Where(n => n.Protocol == protocol);
        }

        // Filter by IsOnline
        if (queryParams.Filters?.TryGetValue("isOnline", out var isOnlineFilter) == true &&
            bool.TryParse(isOnlineFilter, out var isOnline))
        {
            query = query.Where(n => n.IsOnline == isOnline);
        }

        // Date filter on LastSeen
        query = query.ApplyDateFilter(queryParams, n => n.LastSeen);

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Apply sorting (default: Name ascending)
        query = query.ApplySort(queryParams, "Name");

        // Apply paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);

        return PagedResultDto<NodeDto>.Create(
            items.Select(n => n.ToDto()),
            totalRecords,
            queryParams);
    }

    /// <inheritdoc />
    public async Task<NodeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        return node?.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto?> GetByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == nodeId, ct);

        return node?.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto> GetOrCreateByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default)
    {
        var existingNode = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == nodeId, ct);

        if (existingNode != null)
        {
            // Update LastSeen and Online status
            existingNode.LastSeen = DateTime.UtcNow;
            existingNode.IsOnline = true;
            await _unitOfWork.SaveChangesAsync(ct);

            return existingNode.ToDto();
        }

        // Create new Node (Auto-Registration)
        var newNode = new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = nodeId,
            Name = GenerateNameFromNodeId(nodeId),
            Protocol = Protocol.WLAN,
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Nodes.Add(newNode);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node auto-registered: {NodeId} ({Name}) for Hub {HubId}",
            newNode.NodeId, newNode.Name, hubId);

        return newNode.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto> CreateAsync(CreateNodeDto dto, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        // Check if NodeId already exists within the Hub
        var exists = await _context.Nodes
            .AsNoTracking()
            .AnyAsync(n => n.HubId == hubId && n.NodeId == dto.NodeId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Node with NodeId '{dto.NodeId}' already exists for this Hub.");
        }

        var node = dto.ToEntity(hubId);

        _context.Nodes.Add(node);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node created: {NodeId} ({Name})", node.NodeId, node.Name);

        return node.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto?> UpdateAsync(Guid id, UpdateNodeDto dto, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {NodeId}", id);
            return null;
        }

        node.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node updated: {NodeId}", id);

        return node.ToDto();
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.LastSeen = DateTime.UtcNow;
        node.IsOnline = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .Include(n => n.Hub)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.IsOnline = isOnline;

        if (!isOnline)
        {
            _logger.LogWarning("Node offline: {NodeId} ({Name})", node.NodeId, node.Name);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, NodeStatusDto status, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.ApplyStatus(status);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<NodeDto> RegisterOrUpdateAsync(CreateNodeDto dto, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        var existingNode = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == dto.NodeId, ct);

        if (existingNode != null)
        {
            // Update existing node
            existingNode.LastSeen = DateTime.UtcNow;
            existingNode.IsOnline = true;

            if (!string.IsNullOrEmpty(dto.Name))
                existingNode.Name = dto.Name;

            if (dto.Location != null)
                existingNode.Location = dto.Location.ToEntity();

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogDebug("Node updated via registration: {NodeId}", dto.NodeId);
            return existingNode.ToDto();
        }

        // Create new Node
        return await CreateAsync(dto, ct);
    }

    /// <inheritdoc />
    public async Task<(NodeDto Node, bool IsNew)> RegisterOrUpdateWithStatusAsync(CreateNodeDto dto, string? firmwareVersion = null, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        var existingNode = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == dto.NodeId, ct);

        if (existingNode != null)
        {
            // Update existing node
            existingNode.LastSeen = DateTime.UtcNow;
            existingNode.IsOnline = true;

            if (!string.IsNullOrEmpty(dto.Name))
                existingNode.Name = dto.Name;

            if (dto.Location != null)
                existingNode.Location = dto.Location.ToEntity();

            if (!string.IsNullOrEmpty(firmwareVersion))
                existingNode.FirmwareVersion = firmwareVersion;

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogDebug("Node updated via registration: {NodeId}", dto.NodeId);
            return (existingNode.ToDto(), false);
        }

        // Create new Node
        var newNode = await CreateAsync(dto, ct);
        return (newNode, true);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return false;

        _context.Nodes.Remove(node);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node deleted: {NodeId} ({Name})", node.NodeId, node.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<NodeSensorsLatestDto?> GetSensorsLatestAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
                .ThenInclude(a => a.Sensor)
                    .ThenInclude(s => s.Capabilities)
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {NodeId}", nodeId);
            return null;
        }

        var sensorReadings = new List<SensorLatestReadingDto>();

        foreach (var assignment in node.SensorAssignments.Where(a => a.IsActive))
        {
            var sensor = assignment.Sensor;

            // Get latest readings for this specific assignment, grouped by measurement type
            var latestReadings = await _context.Readings
                .AsNoTracking()
                .Where(r => r.NodeId == nodeId && r.AssignmentId == assignment.Id)
                .GroupBy(r => r.MeasurementType)
                .Select(g => g.OrderByDescending(r => r.Timestamp).First())
                .ToListAsync(ct);

            // If no readings with AssignmentId, try to find by NodeId and capability measurement types
            // The simulator sends measurementType matching capability types (e.g., "temperature", "humidity", "pressure")
            if (latestReadings.Count == 0 && sensor.Capabilities.Any())
            {
                // Get all measurement types from this sensor's capabilities
                var capabilityTypes = sensor.Capabilities
                    .Select(c => c.MeasurementType.ToLowerInvariant())
                    .ToList();

                // Find latest reading for each capability measurement type
                latestReadings = await _context.Readings
                    .AsNoTracking()
                    .Where(r => r.NodeId == nodeId && capabilityTypes.Contains(r.MeasurementType.ToLower()))
                    .GroupBy(r => r.MeasurementType.ToLower())
                    .Select(g => g.OrderByDescending(r => r.Timestamp).First())
                    .ToListAsync(ct);
            }

            // Build measurements list
            var measurements = latestReadings.Select(reading =>
            {
                // Try to find matching capability for display name and unit
                var capability = sensor.Capabilities
                    .FirstOrDefault(c => c.MeasurementType.Equals(reading.MeasurementType, StringComparison.OrdinalIgnoreCase));

                return new LatestMeasurementDto(
                    ReadingId: reading.Id,
                    MeasurementType: reading.MeasurementType,
                    DisplayName: capability?.DisplayName ?? FormatMeasurementType(reading.MeasurementType),
                    RawValue: reading.RawValue,
                    Value: reading.Value,
                    Unit: !string.IsNullOrEmpty(reading.Unit) ? reading.Unit : (capability?.Unit ?? ""),
                    Timestamp: reading.Timestamp
                );
            }).ToList();

            // Build display name: Alias > Sensor.Name > "SensorCode #EndpointId"
            var displayName = assignment.Alias
                ?? sensor.Name
                ?? $"{sensor.Code} #{assignment.EndpointId}";

            var fullName = sensor.Name ?? $"{sensor.Model ?? sensor.Code} (Endpoint {assignment.EndpointId})";

            sensorReadings.Add(new SensorLatestReadingDto(
                AssignmentId: assignment.Id,
                SensorId: sensor.Id,
                DisplayName: displayName,
                FullName: fullName,
                Alias: assignment.Alias,
                SensorCode: sensor.Code,
                SensorModel: sensor.Model ?? sensor.Code,
                EndpointId: assignment.EndpointId,
                Icon: sensor.Icon,
                Color: sensor.Color,
                IsActive: assignment.IsActive,
                Measurements: measurements
            ));
        }

        // Sort by display name
        var sortedSensors = sensorReadings.OrderBy(s => s.DisplayName).ToList();

        return new NodeSensorsLatestDto(
            NodeId: node.Id,
            NodeName: node.Name,
            LocationName: node.Location?.Name,
            Sensors: sortedSensors
        );
    }

    /// <summary>
    /// Formats a measurement type string to a readable display name.
    /// </summary>
    private static string FormatMeasurementType(string measurementType)
    {
        if (string.IsNullOrEmpty(measurementType)) return "Unknown";

        // Common mappings
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "temperature", "Temperatur" },
            { "humidity", "Luftfeuchtigkeit" },
            { "pressure", "Luftdruck" },
            { "co2", "CO₂" },
            { "pm25", "Feinstaub PM2.5" },
            { "pm10", "Feinstaub PM10" },
            { "light", "Helligkeit" },
            { "lux", "Lux" },
            { "bh1750", "Helligkeit" },
            { "ds18b20", "Temperatur" },
            { "dht22", "Temperatur/Feuchte" },
            { "bme280", "Klima" },
            { "bme680", "Luftqualität" },
            { "soil_moisture", "Bodenfeuchtigkeit" },
            { "battery", "Batterie" },
            { "rssi", "Signalstärke" }
        };

        return mappings.TryGetValue(measurementType, out var displayName)
            ? displayName
            : char.ToUpperInvariant(measurementType[0]) + measurementType[1..].ToLowerInvariant();
    }

    /// <summary>
    /// Generates a name from the NodeId
    /// </summary>
    private static string GenerateNameFromNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return "Unknown Node";

        // "wetterstation-garten-01" -> "Wetterstation Garten 01"
        var parts = nodeId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }

    // === Node Provisioning (BLE Pairing) ===

    /// <inheritdoc />
    public async Task<NodeConfigurationDto> RegisterNodeAsync(
        NodeRegistrationDto dto,
        string wifiSsid,
        string wifiPassword,
        string hubApiUrl,
        CancellationToken ct = default)
    {
        // Check if node already exists by MAC address
        var existingNode = await _context.Nodes
            .FirstOrDefaultAsync(n => n.MacAddress == dto.MacAddress.ToUpperInvariant(), ct);

        if (existingNode != null)
        {
            _logger.LogInformation("Node with MAC {MacAddress} already registered, regenerating API key",
                dto.MacAddress);

            // Generate new API key for existing node
            return await RegenerateApiKeyAsync(existingNode.Id, wifiSsid, wifiPassword, hubApiUrl, ct)
                ?? throw new InvalidOperationException("Failed to regenerate API key");
        }

        // Get default hub (first hub in database)
        var hub = await _context.Hubs.FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No Hub configured. Please create a Hub first.");

        // Generate API key
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var apiKeyHash = ApiKeyGenerator.HashApiKey(apiKey);

        // Create new node
        var node = dto.ToEntity(hub.Id, apiKeyHash);

        _context.Nodes.Add(node);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node registered via BLE: {NodeId} ({Name}) MAC: {MacAddress}",
            node.NodeId, node.Name, node.MacAddress);

        return new NodeConfigurationDto(
            NodeId: node.NodeId,
            ApiKey: apiKey,
            WifiSsid: wifiSsid,
            WifiPassword: wifiPassword,
            HubApiUrl: hubApiUrl
        );
    }

    /// <inheritdoc />
    public async Task<NodeDto?> ValidateApiKeyAsync(string nodeId, string apiKey, CancellationToken ct = default)
    {
        if (!ApiKeyGenerator.IsValidFormat(apiKey))
        {
            _logger.LogWarning("Invalid API key format for node {NodeId}", nodeId);
            return null;
        }

        var node = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.NodeId == nodeId, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {NodeId}", nodeId);
            return null;
        }

        if (!ApiKeyGenerator.ValidateApiKey(apiKey, node.ApiKeyHash))
        {
            _logger.LogWarning("Invalid API key for node {NodeId}", nodeId);
            return null;
        }

        return node.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeHeartbeatResponseDto> ProcessHeartbeatAsync(NodeHeartbeatDto dto, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.NodeId == dto.NodeId, ct);

        if (node == null)
        {
            _logger.LogWarning("Heartbeat from unknown node: {NodeId}", dto.NodeId);
            return new NodeHeartbeatResponseDto(
                Success: false,
                ServerTime: DateTime.UtcNow,
                NextHeartbeatSeconds: 60
            );
        }

        // Update node status
        node.LastSeen = DateTime.UtcNow;
        node.IsOnline = true;

        if (!string.IsNullOrEmpty(dto.FirmwareVersion))
            node.FirmwareVersion = dto.FirmwareVersion;

        if (dto.BatteryLevel.HasValue)
            node.BatteryLevel = dto.BatteryLevel;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug("Heartbeat processed for node {NodeId}", dto.NodeId);

        return new NodeHeartbeatResponseDto(
            Success: true,
            ServerTime: DateTime.UtcNow,
            NextHeartbeatSeconds: 60
        );
    }

    /// <inheritdoc />
    public async Task<NodeDto?> GetByMacAddressAsync(string macAddress, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.MacAddress == macAddress.ToUpperInvariant(), ct);

        return node?.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeConfigurationDto?> RegenerateApiKeyAsync(
        Guid nodeId,
        string wifiSsid,
        string wifiPassword,
        string hubApiUrl,
        CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
        {
            _logger.LogWarning("Cannot regenerate API key: Node not found {NodeId}", nodeId);
            return null;
        }

        // Generate new API key
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var apiKeyHash = ApiKeyGenerator.HashApiKey(apiKey);

        node.ApiKeyHash = apiKeyHash;
        node.Status = NodeStatus.Configured;
        node.LastSeen = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("API key regenerated for node {NodeId}", node.NodeId);

        return new NodeConfigurationDto(
            NodeId: node.NodeId,
            ApiKey: apiKey,
            WifiSsid: wifiSsid,
            WifiPassword: wifiPassword,
            HubApiUrl: hubApiUrl
        );
    }
}
