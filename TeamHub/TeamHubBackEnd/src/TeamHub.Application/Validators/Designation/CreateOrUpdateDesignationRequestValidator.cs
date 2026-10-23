using FluentValidation;
using TeamHub.Application.DTOs.Designation;

namespace TeamHub.Application.Validators.Designation;

public sealed class CreateOrUpdateDesignationRequestValidator
    : AbstractValidator<CreateOrUpdateDesignationRequest>
{
    public CreateOrUpdateDesignationRequestValidator()
    {
        RuleFor(request => request.DesignationName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Designation name is required.")
            .MinimumLength(2).WithMessage("Designation name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Designation name must be 100 characters or fewer.");

        RuleFor(request => request.Rank)
            .GreaterThanOrEqualTo(0).WithMessage("Rank cannot be negative.");
    }
}