using FluentValidation;
using TeamHub.Application.DTOs.CotivityDepartment;

namespace TeamHub.Application.Validators.CotivityDepartment;

public sealed class CreateOrUpdateCotivityDepartmentRequestValidator : AbstractValidator<CreateOrUpdateCotivityDepartmentRequest>
{
    public CreateOrUpdateCotivityDepartmentRequestValidator()
    {
        RuleFor(request => request.CotivityDepartmentName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Cotivity department name is required.")
            .MinimumLength(2).WithMessage("Cotivity department name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Cotivity department name must be 100 characters or fewer.");
    }
}