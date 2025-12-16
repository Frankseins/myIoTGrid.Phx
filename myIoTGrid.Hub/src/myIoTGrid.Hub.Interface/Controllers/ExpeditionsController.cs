using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Expeditions (GPS tracking sessions).
/// Allows users to save, organize and analyze their GPS routes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpeditionsController : ControllerBase
{
    private readonly IExpeditionService _expeditionService;

    public ExpeditionsController(IExpeditionService expeditionService)
    {
        _expeditionService = expeditionService;
    }

    /// <summary>
    /// Creates a new Expedition
    /// </summary>
    /// <param name="dto">Expedition data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Expedition</returns>
    /// <response code="201">Expedition successfully created</response>
    /// <response code="400">Invalid data</response>
    [HttpPost]
    [ProducesResponseType(typeof(ExpeditionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpeditionDto dto, CancellationToken ct)
    {
        try
        {
            var expedition = await _expeditionService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = expedition.Id }, expedition);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Gets all Expeditions with optional filters
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="nodeId">Filter by Node ID</param>
    /// <param name="tags">Filter by tags (comma-separated)</param>
    /// <param name="fromDate">Filter by start date (from)</param>
    /// <param name="toDate">Filter by end date (to)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Expeditions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ExpeditionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ExpeditionStatusDto? status,
        [FromQuery] Guid? nodeId,
        [FromQuery] string? tags,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var filter = new ExpeditionFilterDto(status, nodeId, tags, fromDate, toDate);
        var expeditions = await _expeditionService.GetAllAsync(filter, ct);
        return Ok(expeditions);
    }

    /// <summary>
    /// Gets an Expedition by ID
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Expedition</returns>
    /// <response code="200">Expedition found</response>
    /// <response code="404">Expedition not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpeditionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var expedition = await _expeditionService.GetByIdAsync(id, ct);

        if (expedition == null)
            return NotFound();

        return Ok(expedition);
    }

    /// <summary>
    /// Gets all Expeditions for a specific Node
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Expeditions</returns>
    [HttpGet("node/{nodeId:guid}")]
    [ProducesResponseType(typeof(List<ExpeditionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNode(Guid nodeId, CancellationToken ct)
    {
        var expeditions = await _expeditionService.GetByNodeAsync(nodeId, ct);
        return Ok(expeditions);
    }

    /// <summary>
    /// Updates an Expedition
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Expedition</returns>
    /// <response code="200">Expedition successfully updated</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Expedition not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ExpeditionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpeditionDto dto, CancellationToken ct)
    {
        try
        {
            var expedition = await _expeditionService.UpdateAsync(id, dto, ct);

            if (expedition == null)
                return NotFound();

            return Ok(expedition);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Deletes an Expedition
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Expedition successfully deleted</response>
    /// <response code="404">Expedition not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _expeditionService.DeleteAsync(id, ct);

        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Gets detailed statistics for an Expedition
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Expedition statistics</returns>
    /// <response code="200">Statistics calculated</response>
    /// <response code="404">Expedition not found</response>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(ExpeditionStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(Guid id, CancellationToken ct)
    {
        var stats = await _expeditionService.GetStatisticsAsync(id, ct);

        if (stats == null)
            return NotFound();

        return Ok(stats);
    }

    /// <summary>
    /// Recalculates statistics for an Expedition (e.g., after GPS data changes)
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Expedition</returns>
    /// <response code="200">Statistics recalculated</response>
    /// <response code="404">Expedition not found</response>
    [HttpPost("{id:guid}/recalculate")]
    [ProducesResponseType(typeof(ExpeditionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecalculateStatistics(Guid id, CancellationToken ct)
    {
        var expedition = await _expeditionService.RecalculateStatisticsAsync(id, ct);

        if (expedition == null)
            return NotFound();

        return Ok(expedition);
    }

    /// <summary>
    /// Updates the status of an Expedition
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="status">New status</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Expedition</returns>
    /// <response code="200">Status successfully updated</response>
    /// <response code="404">Expedition not found</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ExpeditionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] ExpeditionStatusDto status, CancellationToken ct)
    {
        var expedition = await _expeditionService.UpdateStatusAsync(id, status, ct);

        if (expedition == null)
            return NotFound();

        return Ok(expedition);
    }

    /// <summary>
    /// Gets GPS data (points with measurements) for an Expedition
    /// </summary>
    /// <param name="id">Expedition ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>GPS data with trail and measurement points</returns>
    /// <response code="200">GPS data returned</response>
    /// <response code="404">Expedition not found</response>
    [HttpGet("{id:guid}/gps")]
    [ProducesResponseType(typeof(ExpeditionGpsDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGpsData(Guid id, CancellationToken ct)
    {
        var gpsData = await _expeditionService.GetGpsDataAsync(id, ct);

        if (gpsData == null)
            return NotFound();

        return Ok(gpsData);
    }
}
