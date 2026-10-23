using FluentValidation;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Validators.Employee;

public sealed class EmployeeAddressDtoValidator : AbstractValidator<EmployeeAddressDto>
{
    public EmployeeAddressDtoValidator()
    {
        RuleFor(address => address.AddressTypeId)
            .GreaterThan(0).WithMessage("Select an address type.");

        RuleFor(address => address.AddressLine1)
            .Cascade(CascadeMode.Stop)
            .Must(line => !string.IsNullOrWhiteSpace(line)).WithMessage("Address line 1 is required.")
            .MaximumLength(160).WithMessage("Address line 1 must be 160 characters or fewer.");

        RuleFor(address => address.AddressLine2)
            .MaximumLength(160).WithMessage("Address line 2 must be 160 characters or fewer.");

        RuleFor(address => address.City)
            .Cascade(CascadeMode.Stop)
            .Must(city => !string.IsNullOrWhiteSpace(city)).WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must be 100 characters or fewer.");

        RuleFor(address => address.State)
            .Cascade(CascadeMode.Stop)
            .Must(state => !string.IsNullOrWhiteSpace(state)).WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State must be 100 characters or fewer.");

        RuleFor(address => address.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(address => address.Longitude)
            .InclusiveBetween(-180, 180);
    }
}