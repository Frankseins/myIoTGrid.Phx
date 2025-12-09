using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for NodeSensorAssignment management (v3.0).
/// Hardware binding of Sensors to Nodes with pin configuration.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public class NodeSensorAssignmentService : INodeSensorAssignmentService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEffectiveConfigService _effectiveConfigService;
    private readonly ILogger<NodeSensorAssignmentService> _logger;

    public NodeSensorAssignmentService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        IEffectiveConfigService effectiveConfigService,
        ILogger<NodeSensorAssignmentService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _effectiveConfigService = effectiveConfigService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeSensorAssignmentDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        var assignments = await _context.NodeSensorAssignments
            .AsNoTracking()
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .Where(a => a.NodeId == nodeId)
            .OrderBy(a => a.EndpointId)
            .ToListAsync(ct);

        return assignments.Select(a => a.ToDto(_effectiveConfigService));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeSensorAssignmentDto>> GetBySensorAsync(Guid sensorId, CancellationToken ct = default)
    {
        var assignments = await _context.NodeSensorAssignments
            .AsNoTracking()
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .Where(a => a.SensorId == sensorId)
            .OrderBy(a => a.Node.Name)
            .ToListAsync(ct);

        return assignments.Select(a => a.ToDto(_effectiveConfigService));
    }

    /// <inheritdoc />
    public async Task<NodeSensorAssignmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await _context.NodeSensorAssignments
            .AsNoTracking()
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return assignment?.ToDto(_effectiveConfigService);
    }

    /// <inheritdoc />
    public async Task<NodeSensorAssignmentDto?> GetByEndpointAsync(Guid nodeId, int endpointId, CancellationToken ct = default)
    {
        var assignment = await _context.NodeSensorAssignments
            .AsNoTracking()
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync(a => a.NodeId == nodeId && a.EndpointId == endpointId, ct);

        return assignment?.ToDto(_effectiveConfigService);
    }

    /// <inheritdoc />
    public async Task<NodeSensorAssignmentDto> CreateAsync(Guid nodeId, CreateNodeSensorAssignmentDto dto, CancellationToken ct = default)
    {
        // Verify Node exists
        var nodeExists = await _context.Nodes
            .AsNoTracking()
            .AnyAsync(n => n.Id == nodeId, ct);

        if (!nodeExists)
        {
            throw new InvalidOperationException($"Node with Id '{nodeId}' not found.");
        }

        // Verify Sensor exists
        var sensorExists = await _context.Sensors
            .AsNoTracking()
            .AnyAsync(s => s.Id == dto.SensorId, ct);

        if (!sensorExists)
        {
            throw new InvalidOperationException($"Sensor with Id '{dto.SensorId}' not found.");
        }

        // Check if EndpointId already exists on this Node
        var endpointExists = await _context.NodeSensorAssignments
            .AsNoTracking()
            .AnyAsync(a => a.NodeId == nodeId && a.EndpointId == dto.EndpointId, ct);

        if (endpointExists)
        {
            throw new InvalidOperationException($"EndpointId '{dto.EndpointId}' already exists on this Node.");
        }

        var assignment = dto.ToEntity(nodeId);

        _context.NodeSensorAssignments.Add(assignment);

        // Update Node status to Configured when first sensor is assigned
        var node = await _context.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node != null && node.Status == NodeStatus.Unconfigured)
        {
            node.Status = NodeStatus.Configured;
            _logger.LogInformation("Node {NodeId} status changed to Configured after first sensor assignment", nodeId);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with navigation properties
        assignment = await _context.NodeSensorAssignments
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .FirstAsync(a => a.Id == assignment.Id, ct);

        _logger.LogInformation("Assignment created: Sensor {SensorId} -> Node {NodeId} (Endpoint {EndpointId})",
            dto.SensorId, nodeId, dto.EndpointId);

        return assignment.ToDto(_effectiveConfigService);
    }

    /// <inheritdoc />
    public async Task<NodeSensorAssignmentDto> UpdateAsync(Guid id, UpdateNodeSensorAssignmentDto dto, CancellationToken ct = default)
    {
        var assignment = await _context.NodeSensorAssignments
            .Include(a => a.Node)
            .Include(a => a.Sensor)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with Id '{id}' not found.");
        }

        assignment.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Assignment updated: {AssignmentId}", id);

        return assignment.ToDto(_effectiveConfigService);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await _context.NodeSensorAssignments
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with Id '{id}' not found.");
        }

        _context.NodeSensorAssignments.Remove(assignment);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Assignment deleted: {AssignmentId}", id);
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await _context.NodeSensorAssignments
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (assignment != null)
        {
            assignment.LastSeenAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
