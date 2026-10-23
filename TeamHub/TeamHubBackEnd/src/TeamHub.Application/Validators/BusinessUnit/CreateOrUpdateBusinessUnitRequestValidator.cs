using FluentValidation;
using TeamHub.Application.DTOs.BusinessUnit;

namespace TeamHub.Application.Validators.BusinessUnit;

public sealed class CreateOrUpdateBusinessUnitRequestValidator : AbstractValidator<CreateOrUpdateBusinessUnitRequest>
{
    public CreateOrUpdateBusinessUnitRequestValidator()
    {
        RuleFor(request => request.BusinessUnitName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Business unit name is required.")
            .MaximumLength(150).WithMessage("Business unit name must be 150 characters or fewer.");
    }
}