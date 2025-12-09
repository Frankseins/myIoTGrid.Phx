using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller für Alert-Typen
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlertTypesController : ControllerBase
{
    private readonly IAlertTypeService _alertTypeService;

    public AlertTypesController(IAlertTypeService alertTypeService)
    {
        _alertTypeService = alertTypeService;
    }

    /// <summary>
    /// Gibt alle Alert-Typen zurück
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Liste aller Alert-Typen</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var alertTypes = await _alertTypeService.GetAllAsync(ct);
        return Ok(alertTypes);
    }

    /// <summary>
    /// Gibt einen Alert-Typ anhand der ID zurück
    /// </summary>
    /// <param name="id">AlertType-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Alert-Typ</returns>
    /// <response code="200">Alert-Typ gefunden</response>
    /// <response code="404">Alert-Typ nicht gefunden</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlertTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var alertType = await _alertTypeService.GetByIdAsync(id, ct);

        if (alertType == null)
            return NotFound();

        return Ok(alertType);
    }

    /// <summary>
    /// Gibt einen Alert-Typ anhand des Codes zurück
    /// </summary>
    /// <param name="code">AlertType-Code (z.B. "mold_risk")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Alert-Typ</returns>
    /// <response code="200">Alert-Typ gefunden</response>
    /// <response code="404">Alert-Typ nicht gefunden</response>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(AlertTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var alertType = await _alertTypeService.GetByCodeAsync(code, ct);

        if (alertType == null)
            return NotFound();

        return Ok(alertType);
    }

    /// <summary>
    /// Erstellt einen neuen Alert-Typ
    /// </summary>
    /// <param name="dto">Alert-Typ-Daten</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der erstellte Alert-Typ</returns>
    /// <response code="201">Alert-Typ erfolgreich erstellt</response>
    /// <response code="400">Ungültige Daten oder Code bereits vorhanden</response>
    [HttpPost]
    [ProducesResponseType(typeof(AlertTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAlertTypeDto dto, CancellationToken ct)
    {
        var alertType = await _alertTypeService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = alertType.Id }, alertType);
    }
}
