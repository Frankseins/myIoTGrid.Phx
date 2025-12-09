using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace myIoTGrid.Hub.Interface.Middleware;

/// <summary>
/// Middleware für Tenant-Handling
/// Extrahiert die TenantId aus dem Request Header oder verwendet den Default-Tenant
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly IConfiguration _configuration;

    private const string TenantIdHeader = "X-Tenant-Id";

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        Guid tenantId;

        // 1. Versuche TenantId aus Header zu lesen
        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var headerValue) &&
            Guid.TryParse(headerValue.FirstOrDefault(), out var parsedId))
        {
            tenantId = parsedId;
            _logger.LogDebug("TenantId aus Header gelesen: {TenantId}", tenantId);
        }
        // 2. Fallback auf Default-Tenant aus Konfiguration
        else
        {
            var defaultTenantIdStr = _configuration["Hub:DefaultTenantId"] ?? "00000000-0000-0000-0000-000000000001";
            tenantId = Guid.Parse(defaultTenantIdStr);
            _logger.LogDebug("Default-TenantId verwendet: {TenantId}", tenantId);
        }

        // TenantId im Service setzen
        tenantService.SetCurrentTenantId(tenantId);

        // Request weiterleiten
        await _next(context);
    }
}

/// <summary>
/// Extension Methods für TenantMiddleware Registration
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
