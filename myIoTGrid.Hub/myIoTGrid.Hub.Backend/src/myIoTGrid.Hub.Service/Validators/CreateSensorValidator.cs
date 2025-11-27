using FluentValidation;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateSensorDto
/// </summary>
public class CreateSensorValidator : AbstractValidator<CreateSensorDto>
{
    public CreateSensorValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty()
            .WithMessage("SensorId is required")
            .MaximumLength(100)
            .WithMessage("SensorId must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("SensorId can only contain letters, numbers, hyphens, and underscores");

        // Either HubId (Guid) or HubIdentifier (string) must be provided
        RuleFor(x => x)
            .Must(x => x.HubId.HasValue || !string.IsNullOrWhiteSpace(x.HubIdentifier))
            .WithMessage("Either HubId or HubIdentifier must be provided");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters");
        });

        When(x => x.HubIdentifier != null, () =>
        {
            RuleFor(x => x.HubIdentifier)
                .MaximumLength(100)
                .WithMessage("HubIdentifier must not exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$")
                .WithMessage("HubIdentifier can only contain letters, numbers, hyphens, and underscores");
        });

        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location!)
                .SetValidator(new LocationValidator());
        });

        When(x => x.SensorTypes != null, () =>
        {
            RuleForEach(x => x.SensorTypes)
                .MaximumLength(50)
                .WithMessage("SensorType must not exceed 50 characters")
                .Matches(@"^[a-z0-9_]+$")
                .WithMessage("SensorType must be lowercase with underscores");
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

        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location!)
                .SetValidator(new LocationValidator());
        });

        When(x => x.FirmwareVersion != null, () =>
        {
            RuleFor(x => x.FirmwareVersion)
                .MaximumLength(50)
                .WithMessage("FirmwareVersion must not exceed 50 characters");
        });

        When(x => x.SensorTypes != null, () =>
        {
            RuleForEach(x => x.SensorTypes)
                .MaximumLength(50)
                .WithMessage("SensorType must not exceed 50 characters")
                .Matches(@"^[a-z0-9_]+$")
                .WithMessage("SensorType must be lowercase with underscores");
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
