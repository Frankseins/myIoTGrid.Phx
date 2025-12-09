using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace myIoTGrid.Hub.Interface.Middleware;

/// <summary>
/// Middleware für globales Exception-Handling
/// Fängt alle unbehandelten Exceptions und gibt ProblemDetails zurück
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid Argument"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        // Logging
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unbehandelte Exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request fehlgeschlagen: {StatusCode} - {Message}", statusCode, exception.Message);
        }

        // ProblemDetails erstellen
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? exception.Message : GetSafeErrorMessage(statusCode),
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        // In Development: Stack Trace hinzufügen
        if (_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        // Trace-ID für Debugging hinzufügen
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static string GetSafeErrorMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Die Anfrage enthält ungültige Daten.",
            HttpStatusCode.NotFound => "Die angeforderte Ressource wurde nicht gefunden.",
            HttpStatusCode.Unauthorized => "Keine Berechtigung für diese Aktion.",
            _ => "Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es später erneut."
        };
    }
}

/// <summary>
/// Extension Methods für ExceptionMiddleware Registration
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}
