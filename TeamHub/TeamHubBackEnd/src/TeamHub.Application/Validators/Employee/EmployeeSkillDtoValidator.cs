using FluentValidation;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Validators.Employee;

public sealed class EmployeeSkillDtoValidator : AbstractValidator<EmployeeSkillDto>
{
    public EmployeeSkillDtoValidator()
    {
        RuleFor(skill => skill.SkillId)
            .GreaterThan(0).WithMessage("Select a valid skill.");

        RuleFor(skill => skill.SkillLevel)
            .InclusiveBetween(1, 5).WithMessage("Skill level must be between 1 and 5.");
    }
}