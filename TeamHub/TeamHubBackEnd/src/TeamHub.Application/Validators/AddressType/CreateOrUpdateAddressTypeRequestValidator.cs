using FluentValidation;
using TeamHub.Application.DTOs.AddressType;

namespace TeamHub.Application.Validators.AddressType;

public sealed class CreateOrUpdateAddressTypeRequestValidator : AbstractValidator<CreateOrUpdateAddressTypeRequest>
{
    public CreateOrUpdateAddressTypeRequestValidator()
    {
        RuleFor(request => request.AddressTypeName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Address type is required.")
            .MaximumLength(100).WithMessage("Address type must be 100 characters or fewer.");
    }
}