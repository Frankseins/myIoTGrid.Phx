using FluentAssertions;
using FluentValidation.TestHelper;
using myIoTGrid.Hub.Service.Validators;

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

#region CreateSensorValidator Tests (v3.0 Two-Tier Model)

public class CreateSensorValidatorTests
{
    private readonly CreateSensorValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange - v3.0: CreateSensorDto now has Code, Name, Protocol, Category (no SensorTypeId)
        var dto = new CreateSensorDto(
            Code: "dht22-wohnzimmer",
            Name: "Living Room Temperature Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate"
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ShouldNotHaveValidationErrors()
    {
        // Arrange - v3.0 model with all fields
        var dto = new CreateSensorDto(
            Code: "bme280-kitchen",
            Name: "Kitchen Climate Sensor",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate",
            Description: "Temperature and humidity sensor",
            SerialNumber: "BME280-001",
            Manufacturer: "Bosch",
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            IntervalSeconds: 60,
            Capabilities: new[]
            {
                new CreateSensorCapabilityDto("temperature", "Temperature", "°C"),
                new CreateSensorCapabilityDto("humidity", "Humidity", "%")
            }
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCode_ShouldHaveValidationError()
    {
        // Arrange - v3.0: Code is required
        var dto = new CreateSensorDto(
            Code: "",
            Name: "Test Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate"
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: "",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate"
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: new string('a', 201),
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate"
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyCategory_ShouldHaveValidationError()
    {
        // Arrange - v3.0: Category is required
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: "Test Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: ""
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidIntervalSeconds_ShouldHaveValidationError(int interval)
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: "Test Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate",
            IntervalSeconds: interval
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WithIntervalSecondsTooLarge_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: "Test Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate",
            IntervalSeconds: 86401 // More than 24 hours
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveAnyValidationError();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(3600)]
    [InlineData(86400)]
    public void Validate_WithValidIntervalSeconds_ShouldNotHaveValidationErrors(int interval)
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "test-sensor",
            Name: "Test Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate",
            IntervalSeconds: interval
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IntervalSeconds);
    }
}

#endregion

#region UpdateSensorValidator Tests (v3.0 Two-Tier Model)

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
        result.ShouldHaveValidationErrorFor(x => x.Name);
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

    [Fact]
    public void Validate_WithAllOptionalFields_ShouldNotHaveValidationErrors()
    {
        // Arrange - v3.0 model
        var dto = new UpdateSensorDto(
            Name: "Updated Sensor",
            Description: "Updated description",
            SerialNumber: "SN-001",
            IntervalSeconds: 120,
            Category: "climate",
            IsActive: true
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region CalibrateSensorValidator Tests

public class CalibrateSensorValidatorTests
{
    private readonly CalibrateSensorValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.0
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAllFields_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: -2.5,
            GainCorrection: 1.05,
            CalibrationNotes: "Calibrated against reference thermometer",
            CalibrationDueAt: DateTime.UtcNow.AddMonths(6)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNaNOffsetCorrection_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: double.NaN,
            GainCorrection: 1.0
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OffsetCorrection)
            .WithErrorMessage("OffsetCorrection must be a valid number");
    }

    [Fact]
    public void Validate_WithInfinityOffsetCorrection_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: double.PositiveInfinity,
            GainCorrection: 1.0
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OffsetCorrection)
            .WithErrorMessage("OffsetCorrection must be a valid number");
    }

    [Fact]
    public void Validate_WithNaNGainCorrection_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.0,
            GainCorrection: double.NaN
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GainCorrection)
            .WithErrorMessage("GainCorrection must be a valid number");
    }

    [Fact]
    public void Validate_WithZeroGainCorrection_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.0,
            GainCorrection: 0.0
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GainCorrection)
            .WithErrorMessage("GainCorrection cannot be zero");
    }

    [Fact]
    public void Validate_WithCalibrationNotesTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.0,
            GainCorrection: 1.0,
            CalibrationNotes: new string('a', 1001)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CalibrationNotes)
            .WithErrorMessage("CalibrationNotes must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithCalibrationDueAtInPast_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.0,
            GainCorrection: 1.0,
            CalibrationDueAt: DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert - Validator uses x.CalibrationDueAt!.Value so property path is "CalibrationDueAt.Value"
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("CalibrationDueAt must be in the future");
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(0.0)]
    [InlineData(10.0)]
    public void Validate_WithValidOffsetCorrection_ShouldNotHaveValidationErrors(double offset)
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: offset,
            GainCorrection: 1.0
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OffsetCorrection);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(-1.0)]
    public void Validate_WithValidGainCorrection_ShouldNotHaveValidationErrors(double gain)
    {
        // Arrange
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.0,
            GainCorrection: gain
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GainCorrection);
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

#region ReadingFilterValidator Tests (New Model)

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
            .WithErrorMessage("Page must be greater than 0");
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
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
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
    public void Validate_WithMeasurementTypeTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new ReadingFilterDto(MeasurementType: new string('a', 51));

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType)
            .WithErrorMessage("MeasurementType must not exceed 50 characters");
    }

    [Theory]
    [InlineData("Temperature")]
    [InlineData("HUMIDITY")]
    [InlineData("soil-moisture")]
    public void Validate_WithInvalidMeasurementTypeFormat_ShouldHaveValidationError(string type)
    {
        // Arrange
        var dto = new ReadingFilterDto(MeasurementType: type);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType)
            .WithErrorMessage("MeasurementType must be lowercase with underscores");
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("soil_moisture")]
    [InlineData("co2")]
    public void Validate_WithValidMeasurementTypeFormat_ShouldNotHaveValidationErrors(string type)
    {
        // Arrange
        var dto = new ReadingFilterDto(MeasurementType: type);

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MeasurementType);
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

    [Fact]
    public void Validate_WithAssignmentId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new ReadingFilterDto(AssignmentId: Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNodeId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new ReadingFilterDto(NodeId: Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion

#region CreateReadingValidator Tests (v3.0 Two-Tier Model)

public class CreateReadingValidatorTests
{
    private readonly CreateReadingValidator _sut = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "sensor-node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAllFields_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "sensor-node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "hub-01",
            Timestamp: DateTime.UtcNow
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyNodeId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NodeId)
            .WithErrorMessage("NodeId is required");
    }

    [Fact]
    public void Validate_WithNodeIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: new string('a', 101),
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NodeId)
            .WithErrorMessage("NodeId must not exceed 100 characters");
    }

    [Theory]
    [InlineData("node with spaces")]
    [InlineData("node.with.dots")]
    [InlineData("node@special")]
    public void Validate_WithInvalidNodeIdFormat_ShouldHaveValidationError(string nodeId)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: nodeId,
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NodeId)
            .WithErrorMessage("NodeId can only contain letters, numbers, hyphens, and underscores");
    }

    [Theory]
    [InlineData("node-01")]
    [InlineData("node_01")]
    [InlineData("NODE123")]
    [InlineData("my-Node_123")]
    public void Validate_WithValidNodeIdFormat_ShouldNotHaveValidationErrors(string nodeId)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: nodeId,
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NodeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidEndpointId_ShouldHaveValidationError(int endpointId)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: endpointId,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

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
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 255,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndpointId)
            .WithErrorMessage("EndpointId must not exceed 254 (Matter limitation)");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(254)]
    public void Validate_WithValidEndpointId_ShouldNotHaveValidationErrors(int endpointId)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: endpointId,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndpointId);
    }

    [Fact]
    public void Validate_WithEmptyMeasurementType_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "",
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType)
            .WithErrorMessage("MeasurementType is required");
    }

    [Fact]
    public void Validate_WithMeasurementTypeTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: new string('a', 51),
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType)
            .WithErrorMessage("MeasurementType must not exceed 50 characters");
    }

    [Theory]
    [InlineData("Temperature")]
    [InlineData("HUMIDITY")]
    [InlineData("soil-moisture")]
    [InlineData("co2.level")]
    public void Validate_WithInvalidMeasurementTypeFormat_ShouldHaveValidationError(string type)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: type,
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType)
            .WithErrorMessage("MeasurementType must be lowercase with underscores (e.g., 'temperature', 'soil_moisture')");
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("soil_moisture")]
    [InlineData("co2")]
    [InlineData("pm25")]
    public void Validate_WithValidMeasurementTypeFormat_ShouldNotHaveValidationErrors(string type)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: type,
            RawValue: 21.5
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MeasurementType);
    }

    [Fact]
    public void Validate_WithNaNRawValue_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: double.NaN
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RawValue)
            .WithErrorMessage("RawValue must be a valid number");
    }

    [Fact]
    public void Validate_WithPositiveInfinityRawValue_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: double.PositiveInfinity
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RawValue)
            .WithErrorMessage("RawValue must be a valid number");
    }

    [Fact]
    public void Validate_WithNegativeInfinityRawValue_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: double.NegativeInfinity
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RawValue)
            .WithErrorMessage("RawValue must be a valid number");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(100)]
    [InlineData(21.5)]
    [InlineData(-40.0)]
    public void Validate_WithValidRawValue_ShouldNotHaveValidationErrors(double value)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: value
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RawValue);
    }

    [Fact]
    public void Validate_WithHubIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: new string('a', 101)
        );

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
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: hubId
        );

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
    public void Validate_WithValidHubIdFormat_ShouldNotHaveValidationErrors(string hubId)
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: hubId
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HubId);
    }

    [Fact]
    public void Validate_WithNullHubId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: null
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithTimestampTooFarInFuture_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: DateTime.UtcNow.AddMinutes(10)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Timestamp.Value")
            .WithErrorMessage("Timestamp cannot be more than 5 minutes in the future");
    }

    [Fact]
    public void Validate_WithTimestampTooFarInPast_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: DateTime.UtcNow.AddYears(-2)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Timestamp.Value")
            .WithErrorMessage("Timestamp cannot be more than 1 year in the past");
    }

    [Fact]
    public void Validate_WithTimestampInValidRange_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: DateTime.UtcNow.AddMinutes(-5)
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullTimestamp_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: null
        );

        // Act
        var result = _sut.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endregion
