using FluentValidation;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateReadingDto.
/// New model: Uses EndpointId + MeasurementType instead of Type + Value.
/// </summary>
public class CreateReadingValidator : AbstractValidator<CreateReadingDto>
{
    public CreateReadingValidator()
    {
        RuleFor(x => x.NodeId)
            .NotEmpty()
            .WithMessage("NodeId is required")
            .MaximumLength(100)
            .WithMessage("NodeId must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("NodeId can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.EndpointId)
            .GreaterThan(0)
            .WithMessage("EndpointId must be greater than 0")
            .LessThanOrEqualTo(254)
            .WithMessage("EndpointId must not exceed 254 (Matter limitation)");

        RuleFor(x => x.MeasurementType)
            .NotEmpty()
            .WithMessage("MeasurementType is required")
            .MaximumLength(50)
            .WithMessage("MeasurementType must not exceed 50 characters")
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("MeasurementType must be lowercase with underscores (e.g., 'temperature', 'soil_moisture')");

        RuleFor(x => x.RawValue)
            .Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("RawValue must be a valid number");

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

/// <summary>
/// Validator for ReadingFilterDto
/// </summary>
public class ReadingFilterValidator : AbstractValidator<ReadingFilterDto>
{
    public ReadingFilterValidator()
    {
        When(x => !string.IsNullOrEmpty(x.MeasurementType), () =>
        {
            RuleFor(x => x.MeasurementType)
                .MaximumLength(50)
                .WithMessage("MeasurementType must not exceed 50 characters")
                .Matches(@"^[a-z0-9_]+$")
                .WithMessage("MeasurementType must be lowercase with underscores");
        });

        When(x => !string.IsNullOrEmpty(x.NodeIdentifier), () =>
        {
            RuleFor(x => x.NodeIdentifier)
                .MaximumLength(100)
                .WithMessage("NodeIdentifier must not exceed 100 characters");
        });

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("PageSize must not exceed 1000");

        When(x => x.From.HasValue && x.To.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.From!.Value <= x.To!.Value)
                .WithMessage("From date must be before or equal to To date");
        });
    }
}
