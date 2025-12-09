using FluentValidation;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateSensorDto (v3.0)
/// Complete sensor definition with hardware configuration.
/// </summary>
public class CreateSensorValidator : AbstractValidator<CreateSensorDto>
{
    public CreateSensorValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required")
            .MaximumLength(50)
            .WithMessage("Code must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Code can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required")
            .MaximumLength(50)
            .WithMessage("Category must not exceed 50 characters");

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters");
        });

        When(x => x.SerialNumber != null, () =>
        {
            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .WithMessage("SerialNumber must not exceed 100 characters");
        });

        When(x => x.Manufacturer != null, () =>
        {
            RuleFor(x => x.Manufacturer)
                .MaximumLength(100)
                .WithMessage("Manufacturer must not exceed 100 characters");
        });

        When(x => x.Model != null, () =>
        {
            RuleFor(x => x.Model)
                .MaximumLength(100)
                .WithMessage("Model must not exceed 100 characters");
        });

        RuleFor(x => x.IntervalSeconds)
            .GreaterThan(0)
            .WithMessage("IntervalSeconds must be greater than 0")
            .LessThanOrEqualTo(86400)
            .WithMessage("IntervalSeconds must not exceed 86400 seconds (24 hours)");

        RuleFor(x => x.MinIntervalSeconds)
            .GreaterThan(0)
            .WithMessage("MinIntervalSeconds must be greater than 0");

        RuleFor(x => x.GainCorrection)
            .NotEqual(0)
            .WithMessage("GainCorrection cannot be zero");
    }
}

/// <summary>
/// Validator for UpdateSensorDto (v3.0)
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

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters");
        });

        When(x => x.SerialNumber != null, () =>
        {
            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .WithMessage("SerialNumber must not exceed 100 characters");
        });

        When(x => x.Category != null, () =>
        {
            RuleFor(x => x.Category)
                .MaximumLength(50)
                .WithMessage("Category must not exceed 50 characters");
        });

        When(x => x.IntervalSeconds.HasValue, () =>
        {
            RuleFor(x => x.IntervalSeconds!.Value)
                .GreaterThan(0)
                .WithMessage("IntervalSeconds must be greater than 0")
                .LessThanOrEqualTo(86400)
                .WithMessage("IntervalSeconds must not exceed 86400 seconds (24 hours)");
        });

        When(x => x.GainCorrection.HasValue, () =>
        {
            RuleFor(x => x.GainCorrection!.Value)
                .NotEqual(0)
                .WithMessage("GainCorrection cannot be zero");
        });
    }
}

/// <summary>
/// Validator for CalibrateSensorDto
/// </summary>
public class CalibrateSensorValidator : AbstractValidator<CalibrateSensorDto>
{
    public CalibrateSensorValidator()
    {
        RuleFor(x => x.OffsetCorrection)
            .Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("OffsetCorrection must be a valid number");

        RuleFor(x => x.GainCorrection)
            .Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("GainCorrection must be a valid number")
            .NotEqual(0)
            .WithMessage("GainCorrection cannot be zero");

        When(x => x.CalibrationNotes != null, () =>
        {
            RuleFor(x => x.CalibrationNotes)
                .MaximumLength(1000)
                .WithMessage("CalibrationNotes must not exceed 1000 characters");
        });

        When(x => x.CalibrationDueAt.HasValue, () =>
        {
            RuleFor(x => x.CalibrationDueAt!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("CalibrationDueAt must be in the future");
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

        RuleFor(x => x)
            .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue) ||
                       (!x.Latitude.HasValue && !x.Longitude.HasValue))
            .WithMessage("Both Latitude and Longitude must be provided together, or neither");
    }
}
