using myIoTGrid.Shared.Common.DTOs.Chart;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for chart data retrieval and processing.
/// </summary>
public interface IChartService
{
    Task<ChartDataDto?> GetChartDataAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        ChartInterval interval,
        CancellationToken ct = default);

    Task<ReadingsListDto> GetReadingsListAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        ReadingsListRequestDto request,
        CancellationToken ct = default);

    Task<byte[]> ExportToCsvAsync(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
}
