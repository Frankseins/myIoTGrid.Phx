using myIoTGrid.Shared.Common.DTOs.Dashboard;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for dashboard data.
/// </summary>
public interface IDashboardService
{
    Task<LocationDashboardDto> GetLocationDashboardAsync(
        SparklinePeriod period = SparklinePeriod.Day,
        CancellationToken ct = default);

    Task<LocationDashboardDto> GetFilteredDashboardAsync(
        DashboardFilterDto filter,
        CancellationToken ct = default);

    Task<DashboardFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);
}
