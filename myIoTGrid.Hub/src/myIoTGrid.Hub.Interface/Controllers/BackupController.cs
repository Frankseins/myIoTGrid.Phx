using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for database backup and restore operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IBackupService backupService,
        IHostApplicationLifetime applicationLifetime,
        ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    /// <summary>
    /// Downloads a backup of the SQLite database.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>SQLite database file</returns>
    /// <response code="200">Backup file</response>
    [HttpGet]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download(CancellationToken ct)
    {
        try
        {
            var backupData = await _backupService.CreateBackupAsync(ct);
            var fileName = $"hub_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";

            _logger.LogInformation("Backup downloaded: {FileName}, Size: {Size} bytes", fileName, backupData.Length);

            return File(backupData, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create backup", message = ex.Message });
        }
    }

    /// <summary>
    /// Uploads and restores a SQLite database backup.
    /// Warning: This will replace the current database!
    /// </summary>
    /// <param name="file">SQLite database file</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Restore successful</response>
    /// <response code="400">Invalid file</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        if (!file.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid file type. Please upload a .db file." });
        }

        try
        {
            await using var stream = file.OpenReadStream();

            // Validate the file first
            if (!await _backupService.ValidateBackupAsync(stream, ct))
            {
                return BadRequest(new { error = "Invalid SQLite database file" });
            }

            // Reset stream and restore
            stream.Position = 0;
            await _backupService.RestoreBackupAsync(stream, ct);

            _logger.LogInformation("Database restored from backup: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            return Ok(new { message = "Database restored successfully. Please restart the application for changes to take full effect." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to restore backup", message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the current database file size.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Database size in bytes</returns>
    [HttpGet("size")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSize(CancellationToken ct)
    {
        var size = await _backupService.GetDatabaseSizeAsync(ct);
        return Ok(new { sizeBytes = size, sizeFormatted = FormatFileSize(size) });
    }

    /// <summary>
    /// Triggers a graceful restart of the Hub backend.
    /// The service manager (Docker/systemd) will automatically restart the application.
    /// </summary>
    /// <returns>Success status</returns>
    /// <response code="200">Restart triggered</response>
    [HttpPost("restart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Restart()
    {
        _logger.LogWarning("Application restart requested via API");

        // Return response before stopping
        Task.Run(async () =>
        {
            // Small delay to allow the response to be sent
            await Task.Delay(500);
            _logger.LogInformation("Initiating graceful shutdown...");
            _applicationLifetime.StopApplication();
        });

        return Ok(new { message = "Restart initiated. The application will restart shortly." });
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
