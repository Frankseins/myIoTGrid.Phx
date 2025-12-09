namespace myIoTGrid.Shared.Common.Constants;

/// <summary>
/// Special tenant identifiers used throughout the system.
/// These are reserved tenant IDs for system-wide resources.
/// </summary>
public static class SpecialTenants
{
    /// <summary>
    /// Shared tenant for global resources (sensors, alert types, etc.)
    /// All tenants can read from this tenant.
    /// </summary>
    public static readonly Guid SHARED =
        new Guid("00000000-0000-0000-0000-000000000000");

    /// <summary>
    /// Template tenant for sensor templates and configurations.
    /// Used for default configurations that can be copied.
    /// </summary>
    public static readonly Guid TEMPLATES =
        new Guid("99999999-9999-9999-9999-999999999999");

    /// <summary>
    /// Checks if the given tenant ID is a special tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID to check</param>
    /// <returns>True if this is a special tenant</returns>
    public static bool IsSpecialTenant(Guid tenantId)
        => tenantId == SHARED || tenantId == TEMPLATES;

    /// <summary>
    /// Checks if the user can write to the given tenant
    /// Special tenants can only be written by system admins
    /// </summary>
    /// <param name="tenantId">The tenant ID to check</param>
    /// <param name="isSystemAdmin">Whether the user is a system admin</param>
    /// <returns>True if the user can write to this tenant</returns>
    public static bool CanWrite(Guid tenantId, bool isSystemAdmin)
    {
        if (!IsSpecialTenant(tenantId))
            return true;
        return isSystemAdmin;
    }
}
