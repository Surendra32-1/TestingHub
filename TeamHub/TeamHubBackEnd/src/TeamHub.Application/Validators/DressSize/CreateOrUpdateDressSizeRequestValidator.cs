using FluentValidation;
using TeamHub.Application.DTOs.DressSize;

namespace TeamHub.Application.Validators.DressSize;

public sealed class CreateOrUpdateDressSizeRequestValidator : AbstractValidator<CreateOrUpdateDressSizeRequest>
{
    public CreateOrUpdateDressSizeRequestValidator()
    {
        RuleFor(request => request.DressName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Dress name is required.")
            .MaximumLength(100).WithMessage("Dress name must be 100 characters or fewer.");

        RuleFor(request => request.Size)
            .Cascade(CascadeMode.Stop)
            .Must(size => !string.IsNullOrWhiteSpace(size)).WithMessage("Dress size is required.")
            .MaximumLength(10).WithMessage("Dress size must be 10 characters or fewer.");
    }
}