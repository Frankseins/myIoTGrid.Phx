using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller für Alerts/Warnungen
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>
    /// Gibt alle aktiven Alerts zurück
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Liste der aktiven Alerts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var alerts = await _alertService.GetActiveAsync(ct);
        return Ok(alerts);
    }

    /// <summary>
    /// Gibt Alerts gefiltert und paginiert zurück
    /// </summary>
    /// <param name="filter">Filterkriterien</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginierte Liste von Alerts</returns>
    [HttpGet("filtered")]
    [ProducesResponseType(typeof(PaginatedResultDto<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered([FromQuery] AlertFilterDto filter, CancellationToken ct)
    {
        var result = await _alertService.GetFilteredAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gibt einen Alert anhand der ID zurück
    /// </summary>
    /// <param name="id">Alert-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Alert</returns>
    /// <response code="200">Alert gefunden</response>
    /// <response code="404">Alert nicht gefunden</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var alert = await _alertService.GetByIdAsync(id, ct);

        if (alert == null)
            return NotFound();

        return Ok(alert);
    }

    /// <summary>
    /// Bestätigt einen Alert (Acknowledge)
    /// </summary>
    /// <param name="id">Alert-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der bestätigte Alert</returns>
    /// <response code="200">Alert erfolgreich bestätigt</response>
    /// <response code="404">Alert nicht gefunden</response>
    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        var alert = await _alertService.AcknowledgeAsync(id, ct);

        if (alert == null)
            return NotFound();

        return Ok(alert);
    }

    /// <summary>
    /// Empfängt einen Alert von der Cloud (intern)
    /// </summary>
    /// <param name="dto">Alert-Daten von der Cloud</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der erstellte Alert</returns>
    /// <response code="201">Alert erfolgreich erstellt</response>
    /// <response code="400">Ungültige Daten</response>
    [HttpPost("receive")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveFromCloud([FromBody] CreateAlertDto dto, CancellationToken ct)
    {
        var alert = await _alertService.CreateFromCloudAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, alert);
    }
}
