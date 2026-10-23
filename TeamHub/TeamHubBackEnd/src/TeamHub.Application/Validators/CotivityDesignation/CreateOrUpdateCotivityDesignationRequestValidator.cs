using FluentValidation;
using TeamHub.Application.DTOs.CotivityDesignation;

namespace TeamHub.Application.Validators.CotivityDesignation;

public sealed class CreateOrUpdateCotivityDesignationRequestValidator : AbstractValidator<CreateOrUpdateCotivityDesignationRequest>
{
    public CreateOrUpdateCotivityDesignationRequestValidator()
    {
        RuleFor(request => request.CotivityDesignationName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Cotivity designation name is required.")
            .MinimumLength(2).WithMessage("Cotivity designation name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Cotivity designation name must be 100 characters or fewer.");

        RuleFor(request => request.CotivityRank)
            .GreaterThanOrEqualTo(0).WithMessage("Rank cannot be negative.");
    }
}