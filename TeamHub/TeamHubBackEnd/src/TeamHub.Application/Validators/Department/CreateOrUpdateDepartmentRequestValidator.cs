using FluentValidation;
using TeamHub.Application.DTOs.Department;

namespace TeamHub.Application.Validators.Department;

public sealed class CreateOrUpdateDepartmentRequestValidator
    : AbstractValidator<CreateOrUpdateDepartmentRequest>
{
    public CreateOrUpdateDepartmentRequestValidator()
    {
        RuleFor(request => request.DepartmentName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Department name is required.")
            .MinimumLength(2).WithMessage("Department name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Department name must be 100 characters or fewer.");
    }
}