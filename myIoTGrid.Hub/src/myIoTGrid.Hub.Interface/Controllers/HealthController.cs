using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller für Health Checks
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basis Health Check - gibt immer Healthy zurück wenn die API erreichbar ist
    /// </summary>
    /// <returns>Health Status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new HealthResponse("Healthy", DateTime.UtcNow));
    }

    /// <summary>
    /// Readiness Check - prüft ob die API bereit ist, Requests zu verarbeiten
    /// </summary>
    /// <returns>Readiness Status</returns>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Ready()
    {
        return Ok(new HealthResponse("Ready", DateTime.UtcNow));
    }
}

/// <summary>
/// Health Response DTO
/// </summary>
public record HealthResponse(string Status, DateTime Timestamp);
