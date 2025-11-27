using FluentValidation;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateSensorDataDto
/// </summary>
public class CreateSensorDataValidator : AbstractValidator<CreateSensorDataDto>
{
    public CreateSensorDataValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty()
            .WithMessage("SensorId is required")
            .MaximumLength(100)
            .WithMessage("SensorId must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("SensorId can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.SensorType)
            .NotEmpty()
            .WithMessage("SensorType is required")
            .MaximumLength(50)
            .WithMessage("SensorType must not exceed 50 characters")
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("SensorType must be lowercase with underscores (e.g., 'temperature', 'soil_moisture')");

        RuleFor(x => x.Value)
            .Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("Value must be a valid number");

        When(x => x.HubId != null, () =>
        {
            RuleFor(x => x.HubId)
                .MaximumLength(100)
                .WithMessage("HubId must not exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$")
                .WithMessage("HubId can only contain letters, numbers, hyphens, and underscores");
        });

        When(x => x.Timestamp.HasValue, () =>
        {
            RuleFor(x => x.Timestamp!.Value)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Timestamp cannot be more than 5 minutes in the future")
                .GreaterThan(DateTime.UtcNow.AddYears(-1))
                .WithMessage("Timestamp cannot be more than 1 year in the past");
        });
    }
}
