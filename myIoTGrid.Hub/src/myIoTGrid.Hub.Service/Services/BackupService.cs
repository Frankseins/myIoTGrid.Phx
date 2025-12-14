using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for SQLite database backup and restore operations.
/// </summary>
public class BackupService : IBackupService
{
    private readonly HubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<BackupService> _logger;
    private readonly string _databasePath;

    public BackupService(
        HubDbContext context,
        IConfiguration configuration,
        IHostApplicationLifetime applicationLifetime,
        ILogger<BackupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _databasePath = GetDatabasePath();
    }

    /// <inheritdoc />
    public async Task<byte[]> CreateBackupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Creating database backup...");

        // Ensure all pending changes are written to disk
        await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);", ct);

        if (!File.Exists(_databasePath))
        {
            throw new FileNotFoundException("Database file not found.", _databasePath);
        }

        // Read the database file
        var backupData = await File.ReadAllBytesAsync(_databasePath, ct);

        _logger.LogInformation("Database backup created successfully. Size: {Size} bytes", backupData.Length);

        return backupData;
    }

    /// <inheritdoc />
    public async Task RestoreBackupAsync(Stream backupStream, CancellationToken ct = default)
    {
        _logger.LogWarning("Starting database restore. This will replace the current database!");

        // Validate the backup first
        if (!await ValidateBackupAsync(backupStream, ct))
        {
            throw new InvalidOperationException("Invalid SQLite database file.");
        }

        // Reset stream position after validation
        backupStream.Position = 0;

        // Create backup of current database before restore
        var currentBackupPath = $"{_databasePath}.restore_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        if (File.Exists(_databasePath))
        {
            File.Copy(_databasePath, currentBackupPath);
            _logger.LogInformation("Current database backed up to: {Path}", currentBackupPath);
        }

        try
        {
            // Close all connections by clearing the connection pool
            await _context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();

            // Write the new database file
            await using var fileStream = new FileStream(_databasePath, FileMode.Create, FileAccess.Write);
            await backupStream.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Database file restored from backup. Scheduling automatic restart...");

            // Schedule automatic restart after a short delay (to allow response to be sent)
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Wait 1 second for response to be sent
                _logger.LogWarning("Initiating automatic restart after database restore...");
                _applicationLifetime.StopApplication();
            });

            _logger.LogInformation("Database restored successfully. Application will restart shortly.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore database. Attempting to recover from backup...");

            // Attempt to restore the original database
            if (File.Exists(currentBackupPath))
            {
                try
                {
                    File.Copy(currentBackupPath, _databasePath, overwrite: true);
                    _logger.LogInformation("Original database recovered from backup.");
                }
                catch (Exception recoveryEx)
                {
                    _logger.LogCritical(recoveryEx, "Failed to recover original database!");
                }
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateBackupAsync(Stream backupStream, CancellationToken ct = default)
    {
        // SQLite files start with "SQLite format 3\0"
        var header = new byte[16];
        var bytesRead = await backupStream.ReadAsync(header, ct);

        if (bytesRead < 16)
        {
            return false;
        }

        // Check SQLite magic header
        var expectedHeader = "SQLite format 3\0"u8.ToArray();
        for (int i = 0; i < expectedHeader.Length; i++)
        {
            if (header[i] != expectedHeader[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public Task<long> GetDatabaseSizeAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_databasePath))
        {
            return Task.FromResult(0L);
        }

        var fileInfo = new FileInfo(_databasePath);
        return Task.FromResult(fileInfo.Length);
    }

    private string GetDatabasePath()
    {
        // Get the connection string from the DbContext's connection
        var connection = _context.Database.GetDbConnection();
        var connectionString = connection.ConnectionString;

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = _configuration.GetConnectionString("HubDb")
                ?? "Data Source=./data/hub.db";
        }

        // Parse the Data Source from connection string
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        _logger.LogDebug("Database path from connection: {DataSource}", dataSource);

        // Resolve relative path using current working directory (not AppContext.BaseDirectory)
        if (!Path.IsPathRooted(dataSource))
        {
            var basePath = Directory.GetCurrentDirectory();
            dataSource = Path.GetFullPath(Path.Combine(basePath, dataSource));
        }

        _logger.LogDebug("Resolved database path: {Path}", dataSource);

        return dataSource;
    }
}
