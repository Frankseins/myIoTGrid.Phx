using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;
using myIoTGrid.Hub.Shared.Enums;

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
    private readonly INodeSensorAssignmentService _assignmentService;
    private readonly IHubService _hubService;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly IConfiguration _configuration;

    public NodesController(
        INodeService nodeService,
        INodeSensorAssignmentService assignmentService,
        IHubService hubService,
        IHubContext<SensorHub> hubContext,
        IConfiguration configuration)
    {
        _nodeService = nodeService;
        _assignmentService = assignmentService;
        _hubService = hubService;
        _hubContext = hubContext;
        _configuration = configuration;
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

    /// <summary>
    /// Returns all Sensor Assignments for a Node with effective configuration
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Assignments with effective config (pins, intervals, calibration)</returns>
    [HttpGet("{id:guid}/assignments")]
    [ProducesResponseType(typeof(IEnumerable<NodeSensorAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(Guid id, CancellationToken ct)
    {
        var assignments = await _assignmentService.GetByNodeAsync(id, ct);
        return Ok(assignments);
    }

    /// <summary>
    /// Returns an Assignment by EndpointId for a Node
    /// </summary>
    /// <param name="id">Node-ID</param>
    /// <param name="endpointId">Matter-konformer EndpointId (1-254)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Assignment with effective configuration</returns>
    [HttpGet("{id:guid}/assignments/endpoint/{endpointId:int}")]
    [ProducesResponseType(typeof(NodeSensorAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignmentByEndpoint(Guid id, int endpointId, CancellationToken ct)
    {
        var assignment = await _assignmentService.GetByEndpointAsync(id, endpointId, ct);

        if (assignment == null)
            return NotFound();

        return Ok(assignment);
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
        if (string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "SerialNumber is required"
            });
        }

        // Get or create default hub for sensor registration
        var defaultHub = await _hubService.GetDefaultHubAsync(ct);

        // Convert RegisterNodeDto to CreateNodeDto
        var createDto = new CreateNodeDto(
            NodeId: dto.SerialNumber,
            Name: dto.Name ?? $"Sensor {dto.SerialNumber}",
            HubId: defaultHub.Id,
            Protocol: dto.HardwareType?.ToUpperInvariant() == "LORA" ? ProtocolDto.LoRaWAN : ProtocolDto.WLAN,
            Location: dto.Location
        );

        var (node, isNew) = await _nodeService.RegisterOrUpdateWithStatusAsync(createDto, dto.FirmwareVersion, ct);

        // Notify clients
        await _hubContext.Clients.Group($"hub:{defaultHub.Id}")
            .SendAsync("NodeRegistered", node, ct);

        // Build sensor configuration from capabilities
        // Enable all sensors that the device reported as capabilities
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

        return Ok(response);
    }

    /// <summary>
    /// Creates a new Node
    /// </summary>
    /// <param name="dto">Node data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Node</returns>
    [HttpPost]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status201Created)]
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
        var node = await _nodeService.CreateAsync(createDto, ct);
        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
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
}
