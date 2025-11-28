using FluentValidation;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateSensorDto (physical sensor chip)
/// </summary>
public class CreateSensorValidator : AbstractValidator<CreateSensorDto>
{
    public CreateSensorValidator()
    {
        RuleFor(x => x.SensorTypeId)
            .NotEmpty()
            .WithMessage("SensorTypeId is required")
            .MaximumLength(50)
            .WithMessage("SensorTypeId must not exceed 50 characters")
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("SensorTypeId must be lowercase with underscores (e.g., 'temperature', 'soil_moisture')");

        RuleFor(x => x.EndpointId)
            .GreaterThan(0)
            .WithMessage("EndpointId must be greater than 0")
            .LessThanOrEqualTo(255)
            .WithMessage("EndpointId must not exceed 255 (Matter limitation)");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters");
        });
    }
}

/// <summary>
/// Validator for UpdateSensorDto
/// </summary>
public class UpdateSensorValidator : AbstractValidator<UpdateSensorDto>
{
    public UpdateSensorValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters");
        });
    }
}

/// <summary>
/// Validator for LocationDto
/// </summary>
public class LocationValidator : AbstractValidator<LocationDto>
{
    public LocationValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage("Location name must not exceed 200 characters");
        });

        When(x => x.Latitude.HasValue, () =>
        {
            RuleFor(x => x.Latitude!.Value)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90");
        });

        When(x => x.Longitude.HasValue, () =>
        {
            RuleFor(x => x.Longitude!.Value)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180");
        });

        // If one coordinate is provided, both must be provided
        RuleFor(x => x)
            .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue) ||
                       (!x.Latitude.HasValue && !x.Longitude.HasValue))
            .WithMessage("Both Latitude and Longitude must be provided together, or neither");
    }
}
