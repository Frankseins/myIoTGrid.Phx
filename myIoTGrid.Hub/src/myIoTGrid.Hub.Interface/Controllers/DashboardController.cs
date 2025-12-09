using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Dashboard data.
/// Provides location-grouped sensor widgets with sparkline data (Home Assistant style).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns dashboard data grouped by location with sparkline data.
    /// </summary>
    /// <param name="period">Time period for sparkline data: Hour, Day (default), Week</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Dashboard with location groups and sensor widgets</returns>
    /// <response code="200">Dashboard data returned successfully</response>
    [HttpGet("locations")]
    [ProducesResponseType(typeof(LocationDashboardDto), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30)] // Cache for 30 seconds
    public async Task<IActionResult> GetLocationDashboard(
        [FromQuery] SparklinePeriod period = SparklinePeriod.Day,
        CancellationToken ct = default)
    {
        var dashboard = await _dashboardService.GetLocationDashboardAsync(period, ct);
        return Ok(dashboard);
    }

    /// <summary>
    /// Returns dashboard data with filters applied.
    /// </summary>
    /// <param name="locations">Filter by locations (comma-separated)</param>
    /// <param name="measurementTypes">Filter by measurement types (comma-separated)</param>
    /// <param name="period">Time period for sparkline data: Hour, Day (default), Week</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Filtered dashboard with location groups and sensor widgets</returns>
    /// <response code="200">Dashboard data returned successfully</response>
    [HttpGet("widgets")]
    [ProducesResponseType(typeof(LocationDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilteredDashboard(
        [FromQuery] string[]? locations = null,
        [FromQuery] string[]? measurementTypes = null,
        [FromQuery] SparklinePeriod period = SparklinePeriod.Day,
        CancellationToken ct = default)
    {
        var filter = new DashboardFilterDto(locations, measurementTypes, period);
        var dashboard = await _dashboardService.GetFilteredDashboardAsync(filter, ct);
        return Ok(dashboard);
    }

    /// <summary>
    /// Returns available filter options for the dashboard.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Available locations and measurement types</returns>
    /// <response code="200">Filter options returned successfully</response>
    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(DashboardFilterOptionsDto), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60)] // Cache for 1 minute
    public async Task<IActionResult> GetFilterOptions(CancellationToken ct = default)
    {
        var options = await _dashboardService.GetFilterOptionsAsync(ct);
        return Ok(options);
    }
}
