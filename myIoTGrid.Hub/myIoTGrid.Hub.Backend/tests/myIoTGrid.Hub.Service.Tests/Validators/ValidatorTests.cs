using FluentAssertions;
using FluentValidation.TestHelper;
using myIoTGrid.Hub.Service.Validators;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Tests.Validators;

#region CreateHubValidator Tests

public class CreateHubValidatorTests
{
    private readonly CreateHubValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "test-hub-01");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHubId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HubId)
            .WithErrorMessage("HubId is required");
    }

    [Fact]
    public void Validate_WithHubIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: new string('a', 101));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HubId)
            .WithErrorMessage("HubId must not exceed 100 characters");
    }

    [Theory]
    [InlineData("hub with spaces")]
    [InlineData("hub.with.dots")]
    [InlineData("hub@special")]
    public void Validate_WithInvalidHubIdFormat_ShouldHaveValidationError(string hubId)
    {
        // Arrange
        var dto = new CreateHubDto(HubId: hubId);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HubId)
            .WithErrorMessage("HubId can only contain letters, numbers, hyphens, and underscores");
    }

    [Theory]
    [InlineData("hub-01")]
    [InlineData("hub_01")]
    [InlineData("HUB123")]
    [InlineData("my-Hub_123")]
    public void Validate_WithValidHubIdFormat_ShouldNotHaveValidationErrors(string hubId)
    {
        // Arrange
        var dto = new CreateHubDto(HubId: hubId);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HubId);
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "test-hub", Name: new string('a', 201));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "test-hub", Description: new string('a', 1001));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithNullOptionalFields_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "test-hub", Name: null, Description: null);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region UpdateHubValidator Tests

public class UpdateHubValidatorTests
{
    private readonly UpdateHubValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Test Hub", Description: "A test hub");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: new string('a', 201));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateHubDto(Description: new string('a', 1001));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithNullFields_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateHubDto();

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region CreateSensorValidator Tests (Sensor = Physical Sensor Chip on Node)

public class CreateSensorValidatorTests
{
    private readonly CreateSensorValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: 1);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidDataAndName_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "humidity", EndpointId: 2, Name: "Living Room Humidity");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySensorTypeId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "", EndpointId: 1);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SensorTypeId)
            .WithErrorMessage("SensorTypeId is required");
    }

    [Fact]
    public void Validate_WithSensorTypeIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: new string('a', 51), EndpointId: 1);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SensorTypeId)
            .WithErrorMessage("SensorTypeId must not exceed 50 characters");
    }

    [Theory]
    [InlineData("Temperature")]
    [InlineData("HUMIDITY")]
    [InlineData("soil-moisture")]
    [InlineData("co2.level")]
    public void Validate_WithInvalidSensorTypeIdFormat_ShouldHaveValidationError(string sensorTypeId)
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: sensorTypeId, EndpointId: 1);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SensorTypeId)
            .WithErrorMessage("SensorTypeId must be lowercase with underscores (e.g., 'temperature', 'soil_moisture')");
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("soil_moisture")]
    [InlineData("co2")]
    [InlineData("pm25")]
    public void Validate_WithValidSensorTypeIdFormat_ShouldNotHaveValidationErrors(string sensorTypeId)
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: sensorTypeId, EndpointId: 1);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SensorTypeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidEndpointId_ShouldHaveValidationError(int endpointId)
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: endpointId);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndpointId)
            .WithErrorMessage("EndpointId must be greater than 0");
    }

    [Fact]
    public void Validate_WithEndpointIdTooLarge_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: 256);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndpointId)
            .WithErrorMessage("EndpointId must not exceed 255 (Matter limitation)");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(255)]
    public void Validate_WithValidEndpointId_ShouldNotHaveValidationErrors(int endpointId)
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: endpointId);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndpointId);
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: 1, Name: new string('a', 201));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_WithNullName_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateSensorDto(SensorTypeId: "temperature", EndpointId: 1, Name: null);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region UpdateSensorValidator Tests (Sensor = Physical Sensor Chip on Node)

public class UpdateSensorValidatorTests
{
    private readonly UpdateSensorValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateSensorDto(Name: "Test Sensor");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithIsActive_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateSensorDto(IsActive: false);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateSensorDto(Name: new string('a', 201));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_WithNullFields_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateSensorDto();

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNameAndIsActive_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateSensorDto(Name: "New Sensor Name", IsActive: true);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region LocationValidator Tests

public class LocationValidatorTests
{
    private readonly LocationValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new LocationDto(Name: "Living Room", Latitude: 50.9375, Longitude: 6.9603);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNameOnly_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new LocationDto(Name: "Living Room");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new LocationDto(Name: new string('a', 201));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Location name must not exceed 200 characters");
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_WithInvalidLatitude_ShouldHaveValidationError(double latitude)
    {
        // Arrange
        var dto = new LocationDto(Latitude: latitude, Longitude: 0);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Latitude.Value")
            .WithErrorMessage("Latitude must be between -90 and 90");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_WithInvalidLongitude_ShouldHaveValidationError(double longitude)
    {
        // Arrange
        var dto = new LocationDto(Latitude: 0, Longitude: longitude);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Longitude.Value")
            .WithErrorMessage("Longitude must be between -180 and 180");
    }

    [Fact]
    public void Validate_WithLatitudeOnlyProvided_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new LocationDto(Latitude: 50.0);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Both Latitude and Longitude must be provided together, or neither");
    }

    [Fact]
    public void Validate_WithLongitudeOnlyProvided_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new LocationDto(Longitude: 6.0);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Both Latitude and Longitude must be provided together, or neither");
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Validate_WithBoundaryCoordinates_ShouldNotHaveValidationErrors(double lat, double lon)
    {
        // Arrange
        var dto = new LocationDto(Latitude: lat, Longitude: lon);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region CreateAlertValidator Tests

public class CreateAlertValidatorTests
{
    private readonly CreateAlertValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "High humidity detected");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyAlertTypeCode_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "", Message: "Test message");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertTypeCode)
            .WithErrorMessage("AlertTypeCode is required");
    }

    [Fact]
    public void Validate_WithAlertTypeCodeTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: new string('a', 51), Message: "Test");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertTypeCode)
            .WithErrorMessage("AlertTypeCode must not exceed 50 characters");
    }

    [Theory]
    [InlineData("MoldRisk")]
    [InlineData("mold-risk")]
    [InlineData("mold.risk")]
    public void Validate_WithInvalidAlertTypeCodeFormat_ShouldHaveValidationError(string code)
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: code, Message: "Test message");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertTypeCode)
            .WithErrorMessage("AlertTypeCode must be lowercase with underscores (e.g., 'mold_risk', 'frost_warning')");
    }

    [Fact]
    public void Validate_WithEmptyMessage_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "");

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
            .WithErrorMessage("Message is required");
    }

    [Fact]
    public void Validate_WithMessageTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: new string('a', 1001));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
            .WithErrorMessage("Message must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithHubIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            HubId: new string('a', 101)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HubId)
            .WithErrorMessage("HubId must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithNodeIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            NodeId: new string('a', 101)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NodeId)
            .WithErrorMessage("NodeId must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithRecommendationTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            Recommendation: new string('a', 2001)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recommendation)
            .WithErrorMessage("Recommendation must not exceed 2000 characters");
    }

    [Fact]
    public void Validate_WithExpiresAtInPast_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            ExpiresAt: DateTime.UtcNow.AddHours(-1)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("ExpiresAt.Value")
            .WithErrorMessage("ExpiresAt must be in the future");
    }

    [Fact]
    public void Validate_WithExpiresAtInFuture_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ExpiresAt.Value");
    }

    [Fact]
    public void Validate_WithInvalidAlertLevel_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test",
            Level: (AlertLevelDto)999
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Level)
            .WithErrorMessage("Invalid AlertLevel");
    }

    [Theory]
    [InlineData(AlertLevelDto.Ok)]
    [InlineData(AlertLevelDto.Info)]
    [InlineData(AlertLevelDto.Warning)]
    [InlineData(AlertLevelDto.Critical)]
    public void Validate_WithValidAlertLevel_ShouldNotHaveValidationErrors(AlertLevelDto level)
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Test", Level: level);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Level);
    }
}

#endregion

#region AlertFilterValidator Tests

public class AlertFilterValidatorTests
{
    private readonly AlertFilterValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new AlertFilterDto(Page: 1, PageSize: 20);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPage_ShouldHaveValidationError(int page)
    {
        // Arrange
        var dto = new AlertFilterDto(Page: page);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_WithInvalidPageSize_ShouldHaveValidationError(int pageSize)
    {
        // Arrange
        var dto = new AlertFilterDto(PageSize: pageSize);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 100");
    }

    [Fact]
    public void Validate_WithAlertTypeCodeTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new AlertFilterDto(AlertTypeCode: new string('a', 51));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AlertTypeCode)
            .WithErrorMessage("AlertTypeCode must not exceed 50 characters");
    }

    [Fact]
    public void Validate_WithFromAfterTo_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new AlertFilterDto(
            From: DateTime.UtcNow.AddDays(1),
            To: DateTime.UtcNow
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("From date must be before or equal to To date");
    }

    [Fact]
    public void Validate_WithFromBeforeTo_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new AlertFilterDto(
            From: DateTime.UtcNow,
            To: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithFromEqualsTo_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new AlertFilterDto(From: now, To: now);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region ReadingFilterValidator Tests

public class ReadingFilterValidatorTests
{
    private readonly ReadingFilterValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new ReadingFilterDto(Page: 1, PageSize: 100);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPage_ShouldHaveValidationError(int page)
    {
        // Arrange
        var dto = new ReadingFilterDto(Page: page);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Validate_WithInvalidPageSize_ShouldHaveValidationError(int pageSize)
    {
        // Arrange
        var dto = new ReadingFilterDto(PageSize: pageSize);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 1000");
    }

    [Fact]
    public void Validate_WithNodeIdentifierTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new ReadingFilterDto(NodeIdentifier: new string('a', 101));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NodeIdentifier)
            .WithErrorMessage("NodeIdentifier must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithSensorTypeIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new ReadingFilterDto(SensorTypeId: new string('a', 51));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SensorTypeId)
            .WithErrorMessage("SensorTypeId must not exceed 50 characters");
    }

    [Fact]
    public void Validate_WithFromAfterTo_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new ReadingFilterDto(
            From: DateTime.UtcNow.AddDays(1),
            To: DateTime.UtcNow
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("From date must be before or equal to To date");
    }

    [Fact]
    public void Validate_WithFromBeforeTo_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new ReadingFilterDto(
            From: DateTime.UtcNow,
            To: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Validate_WithValidPageSize_ShouldNotHaveValidationErrors(int pageSize)
    {
        // Arrange
        var dto = new ReadingFilterDto(PageSize: pageSize);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }
}

#endregion
