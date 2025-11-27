using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller für Sensor-Typen
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensorTypesController : ControllerBase
{
    private readonly ISensorTypeService _sensorTypeService;

    public SensorTypesController(ISensorTypeService sensorTypeService)
    {
        _sensorTypeService = sensorTypeService;
    }

    /// <summary>
    /// Gibt alle Sensor-Typen zurück
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Liste aller Sensor-Typen</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sensorTypes = await _sensorTypeService.GetAllAsync(ct);
        return Ok(sensorTypes);
    }

    /// <summary>
    /// Gibt einen Sensor-Typ anhand der ID zurück
    /// </summary>
    /// <param name="id">SensorType-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Sensor-Typ</returns>
    /// <response code="200">Sensor-Typ gefunden</response>
    /// <response code="404">Sensor-Typ nicht gefunden</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.GetByIdAsync(id, ct);

        if (sensorType == null)
            return NotFound();

        return Ok(sensorType);
    }

    /// <summary>
    /// Gibt einen Sensor-Typ anhand des Codes zurück
    /// </summary>
    /// <param name="code">SensorType-Code (z.B. "temperature")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Sensor-Typ</returns>
    /// <response code="200">Sensor-Typ gefunden</response>
    /// <response code="404">Sensor-Typ nicht gefunden</response>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.GetByCodeAsync(code, ct);

        if (sensorType == null)
            return NotFound();

        return Ok(sensorType);
    }

    /// <summary>
    /// Erstellt einen neuen Sensor-Typ
    /// </summary>
    /// <param name="dto">Sensor-Typ-Daten</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der erstellte Sensor-Typ</returns>
    /// <response code="201">Sensor-Typ erfolgreich erstellt</response>
    /// <response code="400">Ungültige Daten oder Code bereits vorhanden</response>
    [HttpPost]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorTypeDto dto, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = sensorType.Id }, sensorType);
    }
}
