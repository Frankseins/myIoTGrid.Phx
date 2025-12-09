using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for NodeSensorAssignment management (v3.0).
/// Hardware binding of Sensors to Nodes with pin configuration.
/// </summary>
public interface INodeSensorAssignmentService
{
    Task<IEnumerable<NodeSensorAssignmentDto>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default);
    Task<IEnumerable<NodeSensorAssignmentDto>> GetBySensorAsync(Guid sensorId, CancellationToken ct = default);
    Task<NodeSensorAssignmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NodeSensorAssignmentDto?> GetByEndpointAsync(Guid nodeId, int endpointId, CancellationToken ct = default);
    Task<NodeSensorAssignmentDto> CreateAsync(Guid nodeId, CreateNodeSensorAssignmentDto dto, CancellationToken ct = default);
    Task<NodeSensorAssignmentDto> UpdateAsync(Guid id, UpdateNodeSensorAssignmentDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);
}
