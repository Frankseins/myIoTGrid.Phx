namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for SQLite database backup and restore operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup of the SQLite database and returns it as a byte array.
    /// </summary>
    Task<byte[]> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>
    /// Restores the SQLite database from a backup stream.
    /// Warning: This will replace the current database!
    /// </summary>
    Task RestoreBackupAsync(Stream backupStream, CancellationToken ct = default);

    /// <summary>
    /// Validates if the provided stream is a valid SQLite database.
    /// </summary>
    Task<bool> ValidateBackupAsync(Stream backupStream, CancellationToken ct = default);

    /// <summary>
    /// Gets the current database file size in bytes.
    /// </summary>
    Task<long> GetDatabaseSizeAsync(CancellationToken ct = default);
}
