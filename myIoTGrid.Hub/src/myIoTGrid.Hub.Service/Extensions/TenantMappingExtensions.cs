
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions für Tenant
/// </summary>
public static class TenantMappingExtensions
{
    /// <summary>
    /// Konvertiert Tenant Entity zu TenantDto
    /// </summary>
    public static TenantDto ToDto(this Tenant entity)
    {
        return new TenantDto(
            entity.Id,
            entity.Name,
            MaskApiKey(entity.CloudApiKey),
            entity.CreatedAt,
            entity.LastSyncAt,
            entity.IsActive
        );
    }

    /// <summary>
    /// Konvertiert CreateTenantDto zu Tenant Entity
    /// </summary>
    public static Tenant ToEntity(this CreateTenantDto dto)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CloudApiKey = dto.CloudApiKey,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Aktualisiert ein Tenant Entity mit UpdateTenantDto
    /// </summary>
    public static void ApplyUpdate(this Tenant entity, UpdateTenantDto dto)
    {
        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.CloudApiKey != null)
            entity.CloudApiKey = dto.CloudApiKey;
        if (dto.IsActive.HasValue)
            entity.IsActive = dto.IsActive.Value;
    }

    /// <summary>
    /// Maskiert den API-Key für die Anzeige
    /// </summary>
    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return null;
        if (apiKey.Length <= 8) return "****";
        return apiKey[..4] + "****" + apiKey[^4..];
    }
}
