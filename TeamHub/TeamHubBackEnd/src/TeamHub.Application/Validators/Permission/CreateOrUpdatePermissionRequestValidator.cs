using FluentValidation;
using TeamHub.Application.DTOs.Permission;

namespace TeamHub.Application.Validators.Permission;

public sealed class CreateOrUpdatePermissionRequestValidator : AbstractValidator<CreateOrUpdatePermissionRequest>
{
    public CreateOrUpdatePermissionRequestValidator()
    {
        RuleFor(request => request.PermissionCode)
            .Cascade(CascadeMode.Stop)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithMessage("Permission code is required.")
            .MaximumLength(100).WithMessage("Permission code must be 100 characters or fewer.")
            .Matches(@"^[A-Za-z0-9._:-]+$").WithMessage("Permission code may only contain letters, numbers, dots, colons, dashes and underscores.");

        RuleFor(request => request.PermissionName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Permission name is required.")
            .MaximumLength(150).WithMessage("Permission name must be 150 characters or fewer.");

        RuleFor(request => request.Module)
            .MaximumLength(100).WithMessage("Module must be 100 characters or fewer.");

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or fewer.");
    }
}
