using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

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

    public NodesController(
        INodeService nodeService,
        INodeSensorAssignmentService assignmentService,
        IHubService hubService,
        IHubContext<SensorHub> hubContext)
    {
        _nodeService = nodeService;
        _assignmentService = assignmentService;
        _hubService = hubService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Returns all Nodes for a Hub
    /// </summary>
    /// <param name="hubId">Hub-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Nodes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid hubId, CancellationToken ct)
    {
        var nodes = await _nodeService.GetByHubAsync(hubId, ct);
        return Ok(nodes);
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
    /// Registers or updates a Node (auto-registration)
    /// </summary>
    /// <param name="dto">Node data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The registered/updated Node</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateNodeDto dto, CancellationToken ct)
    {
        // If HubId is provided as string, look it up
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
        var node = await _nodeService.RegisterOrUpdateAsync(createDto, ct);

        // Notify clients
        await _hubContext.Clients.Group($"hub:{hubId}")
            .SendAsync("NodeRegistered", node, ct);

        return Ok(node);
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
}
