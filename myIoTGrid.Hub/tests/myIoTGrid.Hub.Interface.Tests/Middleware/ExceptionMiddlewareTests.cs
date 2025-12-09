using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Middleware;

namespace myIoTGrid.Hub.Interface.Tests.Middleware;

/// <summary>
/// Tests for ExceptionMiddleware.
/// Fängt unbehandelte Exceptions und gibt ProblemDetails zurück.
/// </summary>
public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;

    public ExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
    }

    #region Success Path Tests

    [Fact]
    public async Task InvokeAsync_WithNoException_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationException_Returns400()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Invalid operation");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Bad Request");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Title.Should().Be("Invalid Argument");
    }

    [Fact]
    public async Task InvokeAsync_WithKeyNotFoundException_Returns404()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new KeyNotFoundException("Resource not found");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new UnauthorizedAccessException("Access denied");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_WithUnexpectedException_Returns500()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Unexpected error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    #endregion

    #region Development Environment Tests

    [Fact]
    public async Task InvokeAsync_InDevelopment_IncludesExceptionMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Detailed error message");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Detail.Should().Be("Detailed error message");
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_WithInternalError_IncludesStackTrace()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Internal error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("stackTrace");
    }

    #endregion

    #region Production Environment Tests

    [Fact]
    public async Task InvokeAsync_InProduction_HidesExceptionMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Sensitive internal error details");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Detail.Should().NotContain("Sensitive");
        problemDetails.Detail.Should().Contain("unerwarteter Fehler");
    }

    [Fact]
    public async Task InvokeAsync_InProduction_DoesNotIncludeStackTrace()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Internal error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().NotContain("stackTrace");
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task InvokeAsync_SetsContentTypeToApplicationProblemJson()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_IncludesTraceId()
    {
        // Arrange
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-123";
        var exception = new Exception("Error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("trace-123");
    }

    [Fact]
    public async Task InvokeAsync_IncludesRequestPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/nodes/123";
        var exception = new KeyNotFoundException("Node not found");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Instance.Should().Be("/api/nodes/123");
    }

    [Fact]
    public async Task InvokeAsync_IncludesTypeUrl()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new KeyNotFoundException("Not found");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await ReadProblemDetails(context);
        problemDetails.Type.Should().Be("https://httpstatuses.com/404");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task InvokeAsync_WithInternalError_LogsError()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Internal error");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithBadRequest_LogsWarning()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Bad request");

        RequestDelegate next = ctx => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemDetails(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    #endregion
}
