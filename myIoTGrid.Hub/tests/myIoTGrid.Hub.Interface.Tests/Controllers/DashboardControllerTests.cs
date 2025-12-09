using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for DashboardController.
/// Dashboard provides location-grouped sensor widgets with sparkline data.
/// </summary>
public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardController _sut;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _sut = new DashboardController(_dashboardServiceMock.Object);
    }

    #region GetLocationDashboard Tests

    [Fact]
    public async Task GetLocationDashboard_ReturnsOkWithDashboard()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        _dashboardServiceMock.Setup(s => s.GetLocationDashboardAsync(SparklinePeriod.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetLocationDashboard(SparklinePeriod.Day, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dashboard);
    }

    [Fact]
    public async Task GetLocationDashboard_WithHourPeriod_ReturnsOk()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        _dashboardServiceMock.Setup(s => s.GetLocationDashboardAsync(SparklinePeriod.Hour, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetLocationDashboard(SparklinePeriod.Hour, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _dashboardServiceMock.Verify(s => s.GetLocationDashboardAsync(SparklinePeriod.Hour, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLocationDashboard_WithWeekPeriod_ReturnsOk()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        _dashboardServiceMock.Setup(s => s.GetLocationDashboardAsync(SparklinePeriod.Week, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetLocationDashboard(SparklinePeriod.Week, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _dashboardServiceMock.Verify(s => s.GetLocationDashboardAsync(SparklinePeriod.Week, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLocationDashboard_WithEmptyData_ReturnsOkWithEmptyDashboard()
    {
        // Arrange
        var dashboard = new LocationDashboardDto(new List<LocationGroupDto>());
        _dashboardServiceMock.Setup(s => s.GetLocationDashboardAsync(SparklinePeriod.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetLocationDashboard(SparklinePeriod.Day, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDashboard = okResult.Value.Should().BeOfType<LocationDashboardDto>().Subject;
        returnedDashboard.Locations.Should().BeEmpty();
    }

    #endregion

    #region GetFilteredDashboard Tests

    [Fact]
    public async Task GetFilteredDashboard_WithNoFilters_ReturnsOk()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        _dashboardServiceMock.Setup(s => s.GetFilteredDashboardAsync(It.IsAny<DashboardFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetFilteredDashboard(null, null, SparklinePeriod.Day, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFilteredDashboard_WithLocationFilter_PassesFilterToService()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        DashboardFilterDto? capturedFilter = null;
        _dashboardServiceMock.Setup(s => s.GetFilteredDashboardAsync(It.IsAny<DashboardFilterDto>(), It.IsAny<CancellationToken>()))
            .Callback<DashboardFilterDto, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(dashboard);

        var locations = new[] { "Living Room", "Bedroom" };

        // Act
        await _sut.GetFilteredDashboard(locations, null, SparklinePeriod.Day, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Locations.Should().BeEquivalentTo(locations);
    }

    [Fact]
    public async Task GetFilteredDashboard_WithMeasurementTypeFilter_PassesFilterToService()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        DashboardFilterDto? capturedFilter = null;
        _dashboardServiceMock.Setup(s => s.GetFilteredDashboardAsync(It.IsAny<DashboardFilterDto>(), It.IsAny<CancellationToken>()))
            .Callback<DashboardFilterDto, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(dashboard);

        var measurementTypes = new[] { "temperature", "humidity" };

        // Act
        await _sut.GetFilteredDashboard(null, measurementTypes, SparklinePeriod.Day, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.MeasurementTypes.Should().BeEquivalentTo(measurementTypes);
    }

    [Fact]
    public async Task GetFilteredDashboard_WithBothFilters_PassesBothToService()
    {
        // Arrange
        var dashboard = CreateLocationDashboardDto();
        DashboardFilterDto? capturedFilter = null;
        _dashboardServiceMock.Setup(s => s.GetFilteredDashboardAsync(It.IsAny<DashboardFilterDto>(), It.IsAny<CancellationToken>()))
            .Callback<DashboardFilterDto, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(dashboard);

        var locations = new[] { "Living Room" };
        var measurementTypes = new[] { "temperature" };

        // Act
        await _sut.GetFilteredDashboard(locations, measurementTypes, SparklinePeriod.Hour, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Locations.Should().BeEquivalentTo(locations);
        capturedFilter!.MeasurementTypes.Should().BeEquivalentTo(measurementTypes);
        capturedFilter!.Period.Should().Be(SparklinePeriod.Hour);
    }

    #endregion

    #region GetFilterOptions Tests

    [Fact]
    public async Task GetFilterOptions_ReturnsOkWithOptions()
    {
        // Arrange
        var options = new DashboardFilterOptionsDto(
            new List<string> { "Living Room", "Bedroom", "Kitchen" },
            new List<string> { "temperature", "humidity", "co2" });
        _dashboardServiceMock.Setup(s => s.GetFilterOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

        // Act
        var result = await _sut.GetFilterOptions(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOptions = okResult.Value.Should().BeOfType<DashboardFilterOptionsDto>().Subject;
        returnedOptions.Locations.Should().HaveCount(3);
        returnedOptions.MeasurementTypes.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFilterOptions_WithEmptyData_ReturnsOkWithEmptyOptions()
    {
        // Arrange
        var options = new DashboardFilterOptionsDto(
            new List<string>(),
            new List<string>());
        _dashboardServiceMock.Setup(s => s.GetFilterOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

        // Act
        var result = await _sut.GetFilterOptions(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOptions = okResult.Value.Should().BeOfType<DashboardFilterOptionsDto>().Subject;
        returnedOptions.Locations.Should().BeEmpty();
        returnedOptions.MeasurementTypes.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static LocationDashboardDto CreateLocationDashboardDto()
    {
        var widget = new SensorWidgetDto(
            WidgetId: "widget-1",
            NodeId: Guid.NewGuid(),
            NodeName: "Sensor 1",
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "temperature",
            SensorName: "BME280",
            LocationName: "Living Room",
            Label: "Temperature",
            Unit: "°C",
            Color: "#FF5733",
            CurrentValue: 21.5,
            LastUpdate: DateTime.UtcNow,
            MinMax: new MinMaxDto(18.0, DateTime.UtcNow.AddHours(-2), 26.0, DateTime.UtcNow.AddHours(-1)),
            DataPoints: new List<SparklinePointDto>
            {
                new(DateTime.UtcNow.AddHours(-1), 20.5),
                new(DateTime.UtcNow, 21.5)
            });

        var heroLocation = new LocationGroupDto(
            LocationName: "Living Room",
            LocationIcon: "🏠",
            IsHero: true,
            Widgets: new List<SensorWidgetDto> { widget });

        var otherLocation = new LocationGroupDto(
            LocationName: "Bedroom",
            LocationIcon: "🛏️",
            IsHero: false,
            Widgets: new List<SensorWidgetDto> { widget });

        return new LocationDashboardDto(
            new List<LocationGroupDto> { heroLocation, otherLocation });
    }

    #endregion
}
