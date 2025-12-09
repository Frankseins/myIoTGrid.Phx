using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Nodes (ESP32/LoRa32 Devices).
/// Matter-konform: Entspricht Matter Nodes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NodesController : ControllerBase
{
    private readonly INodeService _nodeService;
    private readonly IHubService _hubService;
    private readonly INodeSensorAssignmentService _assignmentService;
    private readonly ISensorService _sensorService;
    private readonly IReadingService _readingService;
    private readonly INodeDebugLogService _debugLogService;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NodesController> _logger;

    public NodesController(
        INodeService nodeService,
        IHubService hubService,
        INodeSensorAssignmentService assignmentService,
        ISensorService sensorService,
        IReadingService readingService,
        INodeDebugLogService debugLogService,
        IHubContext<SensorHub> hubContext,
        IConfiguration configuration,
        ILogger<NodesController> logger)
    {
        _nodeService = nodeService;
        _hubService = hubService;
        _assignmentService = assignmentService;
        _sensorService = sensorService;
        _readingService = readingService;
        _debugLogService = debugLogService;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Returns all Nodes for a Hub
    /// </summary>
    /// <param name="hubId">Hub-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Nodes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? hubId, CancellationToken ct)
    {
        if (hubId.HasValue)
        {
            var nodes = await _nodeService.GetByHubAsync(hubId.Value, ct);
            return Ok(nodes);
        }

        var allNodes = await _nodeService.GetAllAsync(ct);
        return Ok(allNodes);
    }

    /// <summary>
    /// Returns Nodes with server-side paging, sorting, and filtering
    /// </summary>
    /// <param name="queryParams">Query parameters (page, size, sort, search, filters)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of Nodes</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResultDto<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] QueryParamsDto queryParams, CancellationToken ct)
    {
        var result = await _nodeService.GetPagedAsync(queryParams, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a Node by ID
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Node</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var node = await _nodeService.GetByIdAsync(id, ct);

        if (node == null)
            return NotFound();

        return Ok(node);
    }

    // NOTE: Assignments endpoints moved to NodeSensorAssignmentsController
    // GET /api/nodes/{nodeId:guid}/assignments
    // GET /api/nodes/{nodeId:guid}/assignments/endpoint/{endpointId:int}

    /// <summary>
    /// Returns the latest readings for each sensor assigned to a node.
    /// Groups by sensor (not by measurement type) to show unique sensors with their last values.
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Node with sensors and their latest readings</returns>
    [HttpGet("{id:guid}/sensors/latest")]
    [ProducesResponseType(typeof(NodeSensorsLatestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSensorsLatest(Guid id, CancellationToken ct)
    {
        var result = await _nodeService.GetSensorsLatestAsync(id, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns the GPS status for a node (aggregated from latest GPS readings).
    /// Provides satellites, fix type, HDOP quality, and last known position.
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>GPS status information</returns>
    [HttpGet("{id:guid}/gps-status")]
    [ProducesResponseType(typeof(NodeGpsStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGpsStatus(Guid id, CancellationToken ct)
    {
        var node = await _nodeService.GetByIdAsync(id, ct);
        if (node == null)
            return NotFound();

        // Get latest readings for this node
        var latestReadings = await _readingService.GetLatestByNodeAsync(id, ct);
        var readings = latestReadings.ToList();

        // Check if node has GPS readings
        var gpsMeasurementTypes = new[] { "gps_satellites", "gps_fix", "gps_hdop", "latitude", "longitude", "altitude", "speed" };
        var hasGps = readings.Any(r => gpsMeasurementTypes.Contains(r.MeasurementType.ToLower()));

        // Extract GPS values
        var satellites = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "gps_satellites");
        var fixType = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "gps_fix");
        var hdop = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "gps_hdop");
        var latitude = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "latitude");
        var longitude = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "longitude");
        var altitude = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "altitude");
        var speed = readings.FirstOrDefault(r => r.MeasurementType.ToLower() == "speed");

        // Calculate HDOP quality
        var hdopValue = hdop?.Value ?? 99.99;
        var hdopQuality = hdopValue switch
        {
            < 1 => "Ideal",
            < 2 => "Excellent",
            < 5 => "Good",
            < 10 => "Moderate",
            < 20 => "Fair",
            _ => "Poor"
        };

        // Calculate fix type text
        var fixValue = (int)(fixType?.Value ?? 0);
        var fixTypeText = fixValue switch
        {
            3 => "3D Fix",
            2 => "2D Fix",
            1 => "GPS Fix",
            _ => "No Fix"
        };

        // Get the most recent update timestamp
        var gpsReadings = readings.Where(r => gpsMeasurementTypes.Contains(r.MeasurementType.ToLower()));
        var lastUpdate = gpsReadings.Any() ? gpsReadings.Max(r => r.Timestamp) : (DateTime?)null;

        var gpsStatus = new NodeGpsStatusDto(
            NodeId: id,
            NodeName: node.Name,
            HasGps: hasGps,
            Satellites: (int)(satellites?.Value ?? 0),
            FixType: fixValue,
            FixTypeText: fixTypeText,
            Hdop: hdopValue,
            HdopQuality: hdopQuality,
            Latitude: latitude?.Value,
            Longitude: longitude?.Value,
            Altitude: altitude?.Value,
            Speed: speed?.Value,
            LastUpdate: lastUpdate
        );

        return Ok(gpsStatus);
    }

    /// <summary>
    /// Registers or updates a Node from sensor device (ESP32/LoRa32)
    /// </summary>
    /// <param name="dto">Sensor registration data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Registration response with node info and sensor configuration</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(NodeRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterNodeDto dto, CancellationToken ct)
    {
        _logger.LogInformation("=== NODE REGISTRATION REQUEST ===");
        _logger.LogInformation("RemoteIP: {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogInformation("SerialNumber: {SerialNumber}", dto.SerialNumber);
        _logger.LogInformation("Name: {Name}", dto.Name);
        _logger.LogInformation("FirmwareVersion: {FirmwareVersion}", dto.FirmwareVersion);
        _logger.LogInformation("HardwareType: {HardwareType}", dto.HardwareType);
        _logger.LogInformation("Capabilities: {Capabilities}", dto.Capabilities != null ? string.Join(", ", dto.Capabilities) : "none");

        if (string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            _logger.LogWarning("Registration rejected: SerialNumber is missing");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "SerialNumber is required"
            });
        }

        // Get or create default hub for sensor registration
        _logger.LogInformation("Getting default hub...");
        var defaultHub = await _hubService.GetDefaultHubAsync(ct);
        _logger.LogInformation("Default hub: {HubId} ({HubName})", defaultHub.Id, defaultHub.Name);

        // Convert RegisterNodeDto to CreateNodeDto
        var createDto = new CreateNodeDto(
            NodeId: dto.SerialNumber,
            Name: dto.Name ?? $"Sensor {dto.SerialNumber}",
            HubId: defaultHub.Id,
            Protocol: dto.HardwareType?.ToUpperInvariant() == "LORA" ? ProtocolDto.LoRaWAN : ProtocolDto.WLAN,
            Location: dto.Location
        );

        _logger.LogInformation("Registering node...");
        var (node, isNew) = await _nodeService.RegisterOrUpdateWithStatusAsync(createDto, dto.FirmwareVersion, ct);
        _logger.LogInformation("Node {Action}: {NodeId} (DB-ID: {DbId})", isNew ? "created" : "updated", node.NodeId, node.Id);

        // Notify clients
        await _hubContext.Clients.Group($"hub:{defaultHub.Id}")
            .SendAsync("NodeRegistered", node, ct);
        _logger.LogInformation("SignalR notification sent to hub:{HubId}", defaultHub.Id);

        // Build sensor configuration from capabilities
        // NOTE: Capabilities should later come from Hub, not from sensor
        var sensors = dto.Capabilities?
            .Select(cap => new SensorConfigDto(Type: cap, Enabled: true, Pin: -1))
            .ToList() ?? [];

        // Build connection configuration - use the request's base URL
        var request = HttpContext.Request;
        var endpoint = $"{request.Scheme}://{request.Host}";
        var connection = new ConnectionConfigDto(Mode: "http", Endpoint: endpoint);

        var response = new NodeRegistrationResponseDto(
            NodeId: node.Id,
            SerialNumber: node.NodeId,
            Name: node.Name,
            Location: node.Location?.Name,
            IntervalSeconds: 60, // Default interval
            Sensors: sensors,
            Connection: connection,
            IsNewNode: isNew,
            Message: isNew ? "Node registered successfully" : "Node updated successfully"
        );

        _logger.LogInformation("=== NODE REGISTRATION COMPLETE ===");
        _logger.LogInformation("Response: NodeId={NodeId}, IsNew={IsNew}, Sensors={SensorCount}",
            response.NodeId, response.IsNewNode, response.Sensors.Count);

        return Ok(response);
    }

    /// <summary>
    /// Creates or updates a Node (GetOrCreate pattern).
    /// If a node with the same NodeId exists, it will be updated and returned.
    /// </summary>
    /// <param name="dto">Node data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created or existing Node</returns>
    [HttpPost]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNodeDto dto, CancellationToken ct)
    {
        var hubId = dto.HubId;
        if (hubId == null && !string.IsNullOrWhiteSpace(dto.HubIdentifier))
        {
            var hub = await _hubService.GetOrCreateByHubIdAsync(dto.HubIdentifier, ct);
            hubId = hub.Id;
        }

        if (hubId == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Either HubId or HubIdentifier must be provided"
            });
        }

        var createDto = dto with { HubId = hubId };

        // Use RegisterOrUpdateWithStatusAsync for GetOrCreate pattern
        // This allows re-provisioning of existing nodes
        var (node, isNew) = await _nodeService.RegisterOrUpdateWithStatusAsync(createDto, null, ct);

        if (isNew)
        {
            return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
        }

        // Return existing node with 200 OK
        _logger.LogInformation("Node already exists, returning existing: {NodeId}", dto.NodeId);
        return Ok(node);
    }

    /// <summary>
    /// Updates a Node
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Node</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNodeDto dto, CancellationToken ct)
    {
        var node = await _nodeService.UpdateAsync(id, dto, ct);

        if (node == null)
            return NotFound();

        return Ok(node);
    }

    /// <summary>
    /// Updates the status of a Node
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="dto">Status data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] NodeStatusDto dto, CancellationToken ct)
    {
        var node = await _nodeService.GetByIdAsync(id, ct);
        if (node == null)
            return NotFound();

        await _nodeService.UpdateStatusAsync(id, dto, ct);

        // Notify clients
        await _hubContext.Clients.Group($"node:{id}")
            .SendAsync("NodeStatusChanged", dto, ct);

        return NoContent();
    }

    /// <summary>
    /// Deletes a Node
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _nodeService.DeleteAsync(id, ct);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // === Node Provisioning Endpoints (BLE Pairing) ===

    /// <summary>
    /// Registers a new node via BLE pairing.
    /// Called by ESP32 when it first connects to Hub via BLE.
    /// Returns WiFi credentials and API key.
    /// </summary>
    /// <param name="dto">Registration data (MAC address, firmware version, name)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Node configuration with WiFi credentials and API key</returns>
    [HttpPost("provision")]
    [ProducesResponseType(typeof(NodeConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Provision([FromBody] NodeRegistrationDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.MacAddress))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "MacAddress is required"
            });
        }

        // Get WiFi configuration from appsettings
        var wifiSsid = _configuration["NodeProvisioning:WifiSsid"]
            ?? throw new InvalidOperationException("NodeProvisioning:WifiSsid not configured");
        var wifiPassword = _configuration["NodeProvisioning:WifiPassword"]
            ?? throw new InvalidOperationException("NodeProvisioning:WifiPassword not configured");

        // Build Hub API URL from current request or configuration
        var hubApiUrl = _configuration["NodeProvisioning:HubApiUrl"]
            ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            var config = await _nodeService.RegisterNodeAsync(dto, wifiSsid, wifiPassword, hubApiUrl, ct);

            // Notify clients about new node
            await _hubContext.Clients.All.SendAsync("NodeProvisioned", config.NodeId, ct);

            return Ok(config);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Processes a heartbeat from a node.
    /// Called periodically by ESP32 to report status and update LastSeen.
    /// </summary>
    /// <param name="dto">Heartbeat data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Heartbeat response with server time</returns>
    [HttpPost("heartbeat")]
    [ProducesResponseType(typeof(NodeHeartbeatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Heartbeat([FromBody] NodeHeartbeatDto dto, CancellationToken ct)
    {
        // Get API key from Authorization header
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "API key required in Authorization header"
            });
        }

        var apiKey = authHeader["Bearer ".Length..];

        // Validate API key
        var node = await _nodeService.ValidateApiKeyAsync(dto.NodeId, apiKey, ct);
        if (node == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid API key or node not found"
            });
        }

        var response = await _nodeService.ProcessHeartbeatAsync(dto, ct);

        // Notify clients about node heartbeat
        await _hubContext.Clients.Group($"node:{node.Id}")
            .SendAsync("NodeHeartbeat", node.Id, ct);

        return Ok(response);
    }

    /// <summary>
    /// Validates a node's API key.
    /// Called by ESP32 after WiFi connection to verify configuration.
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Node data if API key is valid</returns>
    [HttpGet("validate/{nodeId}")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateApiKey(string nodeId, CancellationToken ct)
    {
        // Get API key from Authorization header
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "API key required in Authorization header"
            });
        }

        var apiKey = authHeader["Bearer ".Length..];

        var node = await _nodeService.ValidateApiKeyAsync(nodeId, apiKey, ct);
        if (node == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid API key or node not found"
            });
        }

        return Ok(node);
    }

    /// <summary>
    /// Regenerates the API key for a node.
    /// Used when re-provisioning an existing node.
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>New node configuration with new API key</returns>
    [HttpPost("{id:guid}/regenerate-key")]
    [ProducesResponseType(typeof(NodeConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateApiKey(Guid id, CancellationToken ct)
    {
        var wifiSsid = _configuration["NodeProvisioning:WifiSsid"]
            ?? throw new InvalidOperationException("NodeProvisioning:WifiSsid not configured");
        var wifiPassword = _configuration["NodeProvisioning:WifiPassword"]
            ?? throw new InvalidOperationException("NodeProvisioning:WifiPassword not configured");
        var hubApiUrl = _configuration["NodeProvisioning:HubApiUrl"]
            ?? $"{Request.Scheme}://{Request.Host}";

        var config = await _nodeService.RegenerateApiKeyAsync(id, wifiSsid, wifiPassword, hubApiUrl, ct);

        if (config == null)
            return NotFound();

        return Ok(config);
    }

    /// <summary>
    /// Gets a node by MAC address.
    /// </summary>
    /// <param name="macAddress">MAC address (format: AA:BB:CC:DD:EE:FF)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Node if found</returns>
    [HttpGet("by-mac/{macAddress}")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMacAddress(string macAddress, CancellationToken ct)
    {
        var node = await _nodeService.GetByMacAddressAsync(macAddress, ct);

        if (node == null)
            return NotFound();

        return Ok(node);
    }

    /// <summary>
    /// Gets the full sensor configuration for a node.
    /// Called by sensor devices to retrieve their assigned sensors and pin configurations.
    /// </summary>
    /// <param name="serialNumber">Serial number / NodeId of the sensor device</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Full sensor configuration for the node</returns>
    [HttpGet("{serialNumber}/configuration")]
    [ProducesResponseType(typeof(NodeSensorConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(string serialNumber, CancellationToken ct)
    {
        // Find node by serialNumber (NodeId)
        var nodes = await _nodeService.GetAllAsync(ct);
        var node = nodes.FirstOrDefault(n => n.NodeId == serialNumber);

        if (node == null)
            return NotFound(new ProblemDetails
            {
                Title = "Node Not Found",
                Detail = $"No node found with serial number '{serialNumber}'"
            });

        // Get sensor assignments for this node
        var assignments = await _assignmentService.GetByNodeAsync(node.Id, ct);

        // Build sensor configuration list with capabilities
        var sensorConfigs = new List<SensorAssignmentConfigDto>();

        foreach (var a in assignments.Where(a => a.IsActive))
        {
            // Get the full sensor with capabilities
            var sensor = await _sensorService.GetByIdAsync(a.SensorId, ct);
            var capabilities = sensor?.Capabilities?
                .Where(c => c.IsActive)
                .Select(c => new SensorCapabilityConfigDto(
                    MeasurementType: c.MeasurementType,
                    DisplayName: c.DisplayName,
                    Unit: c.Unit
                ))
                .ToList() ?? new List<SensorCapabilityConfigDto>();

            sensorConfigs.Add(new SensorAssignmentConfigDto(
                EndpointId: a.EndpointId,
                SensorCode: a.SensorCode,
                SensorName: a.SensorName,
                Icon: sensor?.Icon,
                Color: sensor?.Color,
                IsActive: a.IsActive,
                IntervalSeconds: a.EffectiveConfig.IntervalSeconds,
                I2CAddress: a.EffectiveConfig.I2CAddress,
                SdaPin: a.EffectiveConfig.SdaPin,
                SclPin: a.EffectiveConfig.SclPin,
                OneWirePin: a.EffectiveConfig.OneWirePin,
                AnalogPin: a.EffectiveConfig.AnalogPin,
                DigitalPin: a.EffectiveConfig.DigitalPin,
                TriggerPin: a.EffectiveConfig.TriggerPin,
                EchoPin: a.EffectiveConfig.EchoPin,
                OffsetCorrection: a.EffectiveConfig.OffsetCorrection,
                GainCorrection: a.EffectiveConfig.GainCorrection,
                Capabilities: capabilities
            ));
        }

        var configuration = new NodeSensorConfigurationDto(
            NodeId: node.Id,
            SerialNumber: node.NodeId,
            Name: node.Name,
            IsSimulation: node.IsSimulation,
            DefaultIntervalSeconds: 60,
            Sensors: sensorConfigs,
            ConfigurationTimestamp: DateTime.UtcNow
        );

        return Ok(configuration);
    }

    // === Debug Configuration (Sprint 8) ===

    /// <summary>
    /// Gets debug configuration for a node.
    /// </summary>
    /// <param name="id">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Debug configuration</returns>
    [HttpGet("{id:guid}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDebugConfiguration(Guid id, CancellationToken ct)
    {
        var config = await _debugLogService.GetDebugConfigurationAsync(id, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node {id} not found" });
        }
        return Ok(config);
    }

    /// <summary>
    /// Sets debug level and remote logging for a node.
    /// </summary>
    /// <param name="id">Node ID</param>
    /// <param name="dto">Debug configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Updated debug configuration</returns>
    [HttpPut("{id:guid}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDebugLevel(Guid id, [FromBody] SetNodeDebugLevelDto dto, CancellationToken ct)
    {
        var config = await _debugLogService.SetDebugLevelAsync(id, dto, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node {id} not found" });
        }

        _logger.LogInformation("Set debug level for node {NodeId}: Level={Level}, RemoteLogging={RemoteLogging}",
            id, dto.DebugLevel, dto.EnableRemoteLogging);

        return Ok(config);
    }
}
