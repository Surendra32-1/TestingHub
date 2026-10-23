using FluentValidation;
using TeamHub.Application.DTOs.Skill;

namespace TeamHub.Application.Validators.Skill;

public sealed class CreateOrUpdateSkillRequestValidator : AbstractValidator<CreateOrUpdateSkillRequest>
{
    public CreateOrUpdateSkillRequestValidator()
    {
        RuleFor(request => request.SkillName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Skill name is required.")
            .MaximumLength(100).WithMessage("Skill name must be 100 characters or fewer.");
    }
}