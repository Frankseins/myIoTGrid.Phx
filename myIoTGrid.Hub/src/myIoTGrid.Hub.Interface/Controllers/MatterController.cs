using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Matter;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// Controller for Matter Bridge operations and commissioning
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatterController : ControllerBase
{
    private readonly IMatterBridgeClient _matterBridgeClient;
    private readonly ILogger<MatterController> _logger;

    public MatterController(
        IMatterBridgeClient matterBridgeClient,
        ILogger<MatterController> logger)
    {
        _matterBridgeClient = matterBridgeClient;
        _logger = logger;
    }

    /// <summary>
    /// Get Matter Bridge status including device count and commission info
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MatterBridgeStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        if (!await _matterBridgeClient.IsAvailableAsync(ct))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Matter Bridge is not available",
                Hint = "Ensure the matter-bridge container is running"
            });
        }

        var status = await _matterBridgeClient.GetStatusAsync(ct);
        if (status == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Could not retrieve Matter Bridge status"
            });
        }

        return Ok(status);
    }

    /// <summary>
    /// Get Matter commissioning information (pairing code, discriminator)
    /// </summary>
    [HttpGet("commission")]
    [ProducesResponseType(typeof(MatterCommissionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetCommissionInfo(CancellationToken ct)
    {
        if (!await _matterBridgeClient.IsAvailableAsync(ct))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Matter Bridge is not available"
            });
        }

        var commissionInfo = await _matterBridgeClient.GetCommissionInfoAsync(ct);
        if (commissionInfo == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Could not retrieve commissioning information"
            });
        }

        return Ok(commissionInfo);
    }

    /// <summary>
    /// Generate QR code for Matter pairing
    /// </summary>
    [HttpPost("commission/qr")]
    [ProducesResponseType(typeof(MatterQrCodeInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateQrCode(CancellationToken ct)
    {
        if (!await _matterBridgeClient.IsAvailableAsync(ct))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Matter Bridge is not available"
            });
        }

        var qrCode = await _matterBridgeClient.GenerateQrCodeAsync(ct);
        if (qrCode == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Could not generate QR code"
            });
        }

        _logger.LogInformation("QR code generated for Matter pairing");
        return Ok(qrCode);
    }

    /// <summary>
    /// Get list of registered Matter devices
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IEnumerable<MatterDeviceInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDevices(CancellationToken ct)
    {
        if (!await _matterBridgeClient.IsAvailableAsync(ct))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Matter Bridge is not available"
            });
        }

        var status = await _matterBridgeClient.GetStatusAsync(ct);
        if (status == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Could not retrieve Matter Bridge status"
            });
        }

        return Ok(status.Devices);
    }

    /// <summary>
    /// Check if Matter Bridge is available
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HealthCheck(CancellationToken ct)
    {
        var isAvailable = await _matterBridgeClient.IsAvailableAsync(ct);

        if (isAvailable)
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "Matter Bridge",
                Timestamp = DateTime.UtcNow
            });
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            Status = "Unhealthy",
            Service = "Matter Bridge",
            Message = "Matter Bridge is not reachable",
            Timestamp = DateTime.UtcNow
        });
    }
}
