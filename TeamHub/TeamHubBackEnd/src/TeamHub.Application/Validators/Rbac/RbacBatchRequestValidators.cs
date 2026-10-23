using FluentValidation;
using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Validators.Rbac;

public sealed class SaveUserRolesRequestValidator : AbstractValidator<SaveUserRolesRequest>
{
    public SaveUserRolesRequestValidator()
    {
        RuleFor(request => request.Assignments)
            .NotNull().WithMessage("Assignments are required.");

        RuleForEach(request => request.Assignments).ChildRules(assignment =>
        {
            assignment.RuleFor(item => item.UserId)
                .GreaterThan(0).WithMessage("User id must be a positive number.");

            assignment.RuleFor(item => item.RoleIds)
                .NotNull().WithMessage("Role ids cannot be null.");

            assignment.RuleFor(item => item.RoleIds)
                .Must(ids => ids is null || ids.All(id => id > 0))
                .WithMessage("Every role id must be a positive number.");

            assignment.RuleFor(item => item.RoleIds)
                .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                .WithMessage("A user cannot be assigned the same role more than once.");
        });

        RuleFor(request => request.Assignments)
            .Must(items => items.DistinctBy(item => item.UserId).Count() == items.Count)
            .WithMessage("Each user may only appear once in the batch.");
    }
}

public sealed class SaveRolePermissionsRequestValidator : AbstractValidator<SaveRolePermissionsRequest>
{
    public SaveRolePermissionsRequestValidator()
    {
        RuleFor(request => request.Assignments)
            .NotNull().WithMessage("Assignments are required.");

        RuleForEach(request => request.Assignments).ChildRules(assignment =>
        {
            assignment.RuleFor(item => item.PermissionId)
                .GreaterThan(0).WithMessage("Permission id must be a positive number.");

            assignment.RuleFor(item => item.RoleIds)
                .NotNull().WithMessage("Role ids cannot be null.");

            assignment.RuleFor(item => item.RoleIds)
                .Must(ids => ids is null || ids.All(id => id > 0))
                .WithMessage("Every role id must be a positive number.");

            assignment.RuleFor(item => item.RoleIds)
                .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                .WithMessage("A permission cannot be granted to the same role more than once.");
        });

        RuleFor(request => request.Assignments)
            .Must(items => items.DistinctBy(item => item.PermissionId).Count() == items.Count)
            .WithMessage("Each permission may only appear once in the batch.");
    }
}
