using FluentValidation;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateAlertDto
/// </summary>
public class CreateAlertValidator : AbstractValidator<CreateAlertDto>
{
    public CreateAlertValidator()
    {
        RuleFor(x => x.AlertTypeCode)
            .NotEmpty()
            .WithMessage("AlertTypeCode is required")
            .MaximumLength(50)
            .WithMessage("AlertTypeCode must not exceed 50 characters")
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("AlertTypeCode must be lowercase with underscores (e.g., 'mold_risk', 'frost_warning')");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required")
            .MaximumLength(1000)
            .WithMessage("Message must not exceed 1000 characters");

        When(x => x.HubId != null, () =>
        {
            RuleFor(x => x.HubId)
                .MaximumLength(100)
                .WithMessage("HubId must not exceed 100 characters");
        });

        When(x => x.NodeId != null, () =>
        {
            RuleFor(x => x.NodeId)
                .MaximumLength(100)
                .WithMessage("NodeId must not exceed 100 characters");
        });

        When(x => x.Recommendation != null, () =>
        {
            RuleFor(x => x.Recommendation)
                .MaximumLength(2000)
                .WithMessage("Recommendation must not exceed 2000 characters");
        });

        When(x => x.ExpiresAt.HasValue, () =>
        {
            RuleFor(x => x.ExpiresAt!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("ExpiresAt must be in the future");
        });

        RuleFor(x => x.Level)
            .IsInEnum()
            .WithMessage("Invalid AlertLevel");
    }
}

/// <summary>
/// Validator for AlertFilterDto
/// </summary>
public class AlertFilterValidator : AbstractValidator<AlertFilterDto>
{
    public AlertFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        When(x => x.AlertTypeCode != null, () =>
        {
            RuleFor(x => x.AlertTypeCode)
                .MaximumLength(50)
                .WithMessage("AlertTypeCode must not exceed 50 characters");
        });

        When(x => x.From.HasValue && x.To.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.From!.Value <= x.To!.Value)
                .WithMessage("From date must be before or equal to To date");
        });
    }
}
