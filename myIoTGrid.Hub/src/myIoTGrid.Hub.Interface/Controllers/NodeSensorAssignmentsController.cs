using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for NodeSensorAssignments (Hardware Binding).
/// Binds Sensors to Nodes with pin configuration and effective config.
/// </summary>
[ApiController]
[Route("api/nodes/{nodeId:guid}/assignments")]
[Produces("application/json")]
public class NodeSensorAssignmentsController : ControllerBase
{
    private readonly INodeSensorAssignmentService _assignmentService;
    private readonly IHubContext<SensorHub> _hubContext;

    public NodeSensorAssignmentsController(
        INodeSensorAssignmentService assignmentService,
        IHubContext<SensorHub> hubContext)
    {
        _assignmentService = assignmentService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Returns all Assignments for a Node
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Assignments with effective configuration</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeSensorAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNode(Guid nodeId, CancellationToken ct)
    {
        var assignments = await _assignmentService.GetByNodeAsync(nodeId, ct);
        return Ok(assignments);
    }

    /// <summary>
    /// Returns an Assignment by ID
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="id">Assignment-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Assignment</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NodeSensorAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid nodeId, Guid id, CancellationToken ct)
    {
        var assignment = await _assignmentService.GetByIdAsync(id, ct);

        if (assignment == null || assignment.NodeId != nodeId)
            return NotFound();

        return Ok(assignment);
    }

    /// <summary>
    /// Returns an Assignment by EndpointId
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="endpointId">Matter-konformer EndpointId (1-254)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Assignment</returns>
    [HttpGet("endpoint/{endpointId:int}")]
    [ProducesResponseType(typeof(NodeSensorAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEndpoint(Guid nodeId, int endpointId, CancellationToken ct)
    {
        var assignment = await _assignmentService.GetByEndpointAsync(nodeId, endpointId, ct);

        if (assignment == null)
            return NotFound();

        return Ok(assignment);
    }

    /// <summary>
    /// Creates a new Assignment (binds a Sensor to a Node)
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="dto">Assignment data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Assignment with effective configuration</returns>
    /// <response code="201">Assignment successfully created</response>
    /// <response code="400">Invalid data or EndpointId already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(NodeSensorAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid nodeId, [FromBody] CreateNodeSensorAssignmentDto dto, CancellationToken ct)
    {
        try
        {
            var assignment = await _assignmentService.CreateAsync(nodeId, dto, ct);

            // Notify clients about new assignment
            await _hubContext.Clients.Group($"node:{nodeId}")
                .SendAsync("AssignmentCreated", assignment, ct);

            return CreatedAtAction(nameof(GetById), new { nodeId, id = assignment.Id }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates an Assignment (pin overrides, interval, etc.)
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="id">Assignment-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Assignment with recalculated effective configuration</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NodeSensorAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid nodeId, Guid id, [FromBody] UpdateNodeSensorAssignmentDto dto, CancellationToken ct)
    {
        try
        {
            var existing = await _assignmentService.GetByIdAsync(id, ct);
            if (existing == null || existing.NodeId != nodeId)
                return NotFound();

            var assignment = await _assignmentService.UpdateAsync(id, dto, ct);

            // Notify clients about updated assignment
            await _hubContext.Clients.Group($"node:{nodeId}")
                .SendAsync("AssignmentUpdated", assignment, ct);

            return Ok(assignment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Deletes an Assignment (unbinds a Sensor from a Node)
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="id">Assignment-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid nodeId, Guid id, CancellationToken ct)
    {
        try
        {
            var existing = await _assignmentService.GetByIdAsync(id, ct);
            if (existing == null || existing.NodeId != nodeId)
                return NotFound();

            await _assignmentService.DeleteAsync(id, ct);

            // Notify clients about deleted assignment
            await _hubContext.Clients.Group($"node:{nodeId}")
                .SendAsync("AssignmentDeleted", id, ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }
}

/// <summary>
/// Alternative route for accessing assignments by sensor
/// </summary>
[ApiController]
[Route("api/sensors/{sensorId:guid}/assignments")]
[Produces("application/json")]
public class SensorAssignmentsController : ControllerBase
{
    private readonly INodeSensorAssignmentService _assignmentService;

    public SensorAssignmentsController(INodeSensorAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Returns all Assignments for a Sensor
    /// </summary>
    /// <param name="sensorId">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Assignments (shows which Nodes use this Sensor)</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeSensorAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySensor(Guid sensorId, CancellationToken ct)
    {
        var assignments = await _assignmentService.GetBySensorAsync(sensorId, ct);
        return Ok(assignments);
    }
}
