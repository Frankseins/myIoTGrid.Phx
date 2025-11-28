using FluentValidation;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for ReadingFilterDto
/// </summary>
public class ReadingFilterValidator : AbstractValidator<ReadingFilterDto>
{
    public ReadingFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 1000)
            .WithMessage("PageSize must be between 1 and 1000");

        When(x => x.NodeIdentifier != null, () =>
        {
            RuleFor(x => x.NodeIdentifier)
                .MaximumLength(100)
                .WithMessage("NodeIdentifier must not exceed 100 characters");
        });

        When(x => x.SensorTypeId != null, () =>
        {
            RuleFor(x => x.SensorTypeId)
                .MaximumLength(50)
                .WithMessage("SensorTypeId must not exceed 50 characters");
        });

        When(x => x.From.HasValue && x.To.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.From!.Value <= x.To!.Value)
                .WithMessage("From date must be before or equal to To date");
        });
    }
}
