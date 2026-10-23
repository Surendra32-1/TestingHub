using FluentValidation;
using TeamHub.Application.DTOs.CotivityEmployee;

namespace TeamHub.Application.Validators.CotivityEmployee;

public sealed class CreateOrUpdateCotivityEmployeeRequestValidator : AbstractValidator<CreateOrUpdateCotivityEmployeeRequest>
{
    public CreateOrUpdateCotivityEmployeeRequestValidator()
    {
        RuleFor(request => request.CotivityEmployeeName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Cotivity employee name is required.")
            .MinimumLength(2).WithMessage("Cotivity employee name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Cotivity employee name must be 150 characters or fewer.");

        RuleFor(request => request.CotivityDepartmentId)
            .GreaterThan(0).WithMessage("Select a Cotivity department.");

        RuleFor(request => request.CotivityDepartmentDesignationId)
            .GreaterThan(0).WithMessage("Select a Cotivity designation.");

        RuleFor(request => request.ReportsToCotivityEmployeeId)
            .GreaterThan(0)
            .WithMessage("Select a valid reporting employee.")
            .When(request => request.ReportsToCotivityEmployeeId.HasValue);
    }
}