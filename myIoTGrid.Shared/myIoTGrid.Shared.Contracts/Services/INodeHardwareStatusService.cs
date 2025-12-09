using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for Node Hardware Status management (Sprint 8).
/// </summary>
public interface INodeHardwareStatusService
{
    Task<NodeHardwareStatusDto?> ReportHardwareStatusAsync(ReportHardwareStatusDto dto, CancellationToken ct = default);
    Task<NodeHardwareStatusDto?> GetHardwareStatusAsync(Guid nodeId, CancellationToken ct = default);
    Task<NodeHardwareStatusDto?> GetHardwareStatusBySerialAsync(string serialNumber, CancellationToken ct = default);
}
