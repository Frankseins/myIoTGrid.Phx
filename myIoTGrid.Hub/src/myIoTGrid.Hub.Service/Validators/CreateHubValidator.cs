using FluentValidation;

namespace myIoTGrid.Hub.Service.Validators;

/// <summary>
/// Validator for CreateHubDto
/// </summary>
public class CreateHubValidator : AbstractValidator<CreateHubDto>
{
    public CreateHubValidator()
    {
        RuleFor(x => x.HubId)
            .NotEmpty()
            .WithMessage("HubId is required")
            .MaximumLength(100)
            .WithMessage("HubId must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("HubId can only contain letters, numbers, hyphens, and underscores");

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
    }
}

/// <summary>
/// Validator for UpdateHubDto
/// </summary>
public class UpdateHubValidator : AbstractValidator<UpdateHubDto>
{
    public UpdateHubValidator()
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
    }
}
