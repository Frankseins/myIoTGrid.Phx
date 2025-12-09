using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for HealthController.
/// </summary>
public class HealthControllerTests
{
    private readonly HealthController _sut;

    public HealthControllerTests()
    {
        _sut = new HealthController();
    }

    #region Get Tests

    [Fact]
    public void Get_ReturnsOkWithHealthyStatus()
    {
        // Act
        var result = _sut.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthResponse>().Subject;
        response.Status.Should().Be("Healthy");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Get_ReturnsTimestampInUtc()
    {
        // Act
        var result = _sut.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthResponse>().Subject;
        response.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region Ready Tests

    [Fact]
    public void Ready_ReturnsOkWithReadyStatus()
    {
        // Act
        var result = _sut.Ready();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthResponse>().Subject;
        response.Status.Should().Be("Ready");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Ready_ReturnsTimestampInUtc()
    {
        // Act
        var result = _sut.Ready();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthResponse>().Subject;
        response.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region HealthResponse Record Tests

    [Fact]
    public void HealthResponse_CanBeCreated()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var response = new HealthResponse("Healthy", timestamp);

        // Assert
        response.Status.Should().Be("Healthy");
        response.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void HealthResponse_SupportsEquality()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var response1 = new HealthResponse("Healthy", timestamp);
        var response2 = new HealthResponse("Healthy", timestamp);

        // Assert
        response1.Should().Be(response2);
    }

    [Fact]
    public void HealthResponse_DifferentStatusNotEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var response1 = new HealthResponse("Healthy", timestamp);
        var response2 = new HealthResponse("Unhealthy", timestamp);

        // Assert
        response1.Should().NotBe(response2);
    }

    #endregion
}
