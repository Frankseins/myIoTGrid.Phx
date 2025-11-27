using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller für Sensor-Messwerte
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService _sensorDataService;

    public SensorDataController(ISensorDataService sensorDataService)
    {
        _sensorDataService = sensorDataService;
    }

    /// <summary>
    /// Erstellt einen neuen Messwert
    /// </summary>
    /// <param name="dto">Messwert-Daten vom Sensor</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der erstellte Messwert</returns>
    /// <response code="201">Messwert erfolgreich erstellt</response>
    /// <response code="400">Ungültige Daten</response>
    [HttpPost]
    [ProducesResponseType(typeof(SensorDataDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorDataDto dto, CancellationToken ct)
    {
        var sensorData = await _sensorDataService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = sensorData.Id }, sensorData);
    }

    /// <summary>
    /// Gibt Messwerte gefiltert und paginiert zurück
    /// </summary>
    /// <param name="filter">Filterkriterien</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginierte Liste von Messwerten</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDto<SensorDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered([FromQuery] SensorDataFilterDto filter, CancellationToken ct)
    {
        var result = await _sensorDataService.GetFilteredAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gibt einen Messwert anhand der ID zurück
    /// </summary>
    /// <param name="id">Messwert-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Der Messwert</returns>
    /// <response code="200">Messwert gefunden</response>
    /// <response code="404">Messwert nicht gefunden</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SensorDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sensorData = await _sensorDataService.GetByIdAsync(id, ct);

        if (sensorData == null)
            return NotFound();

        return Ok(sensorData);
    }

    /// <summary>
    /// Gibt die neuesten Messwerte aller Hubs zurück
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Liste der neuesten Messwerte pro Hub und SensorType</returns>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(IEnumerable<SensorDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var result = await _sensorDataService.GetLatestAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Gibt die neuesten Messwerte eines Hubs zurück
    /// </summary>
    /// <param name="hubId">SensorHub-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Liste der neuesten Messwerte pro SensorType</returns>
    [HttpGet("latest/{hubId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SensorDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestByHub(Guid hubId, CancellationToken ct)
    {
        var result = await _sensorDataService.GetLatestByHubAsync(hubId, ct);
        return Ok(result);
    }
}
