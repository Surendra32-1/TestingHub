using FluentValidation;
using TeamHub.Application.DTOs.BloodGroup;

namespace TeamHub.Application.Validators.BloodGroup;

public sealed class CreateOrUpdateBloodGroupRequestValidator
    : AbstractValidator<CreateOrUpdateBloodGroupRequest>
{
    public CreateOrUpdateBloodGroupRequestValidator()
    {
        RuleFor(request => request.BloodGroupName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Blood group is required.")
            .MaximumLength(5).WithMessage("Blood group must be 5 characters or fewer.");
    }
}