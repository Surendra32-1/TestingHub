using FluentValidation;
using TeamHub.Application.DTOs.Role;

namespace TeamHub.Application.Validators.Role;

public sealed class CreateOrUpdateRoleRequestValidator : AbstractValidator<CreateOrUpdateRoleRequest>
{
    public CreateOrUpdateRoleRequestValidator()
    {
        RuleFor(request => request.RoleName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Role name is required.")
            .MaximumLength(100).WithMessage("Role name must be 100 characters or fewer.");
    }
}