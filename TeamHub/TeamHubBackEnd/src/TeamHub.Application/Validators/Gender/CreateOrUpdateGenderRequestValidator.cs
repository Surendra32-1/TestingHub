using FluentValidation;
using TeamHub.Application.DTOs.Gender;

namespace TeamHub.Application.Validators.Gender;

public sealed class CreateOrUpdateGenderRequestValidator : AbstractValidator<CreateOrUpdateGenderRequest>
{
    public CreateOrUpdateGenderRequestValidator()
    {
        RuleFor(request => request.GenderName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Gender name is required.")
            .MaximumLength(50).WithMessage("Gender name must be 50 characters or fewer.");
    }
}