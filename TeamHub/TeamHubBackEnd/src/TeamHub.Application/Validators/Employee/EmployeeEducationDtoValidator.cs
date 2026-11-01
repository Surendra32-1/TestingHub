using FluentValidation;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Validators.Employee;

public sealed class EmployeeEducationDtoValidator : AbstractValidator<EmployeeEducationDto>
{
    public EmployeeEducationDtoValidator()
    {
        RuleFor(education => education.Qualification)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Qualification is required.")
            .MaximumLength(200).WithMessage("Qualification must be 200 characters or fewer.");

        RuleFor(education => education.BoardUniversity)
            .MaximumLength(200).WithMessage("Board/University must be 200 characters or fewer.");

        RuleFor(education => education.Institution)
            .MaximumLength(200).WithMessage("Institution must be 200 characters or fewer.");

        RuleFor(education => education.Faculty)
            .MaximumLength(100).WithMessage("Faculty must be 100 characters or fewer.");

        RuleFor(education => education.PassedYear)
            .InclusiveBetween(1900, 2200).WithMessage("Passed year must be a valid year.")
            .When(education => education.PassedYear.HasValue);

        RuleFor(education => education.DivisionGrade)
            .MaximumLength(100).WithMessage("Division/Grade must be 100 characters or fewer.");

        RuleFor(education => education.JoinedDateBS)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. joined date must use YYYY-MM-DD format.")
            .When(education => !string.IsNullOrWhiteSpace(education.JoinedDateBS));

        RuleFor(education => education.PassoutDateBS)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. passout date must use YYYY-MM-DD format.")
            .When(education => !string.IsNullOrWhiteSpace(education.PassoutDateBS));

        RuleFor(education => education.PassoutDate)
            .GreaterThanOrEqualTo(education => education.JoinedDate)
            .WithMessage("Passout date cannot be before joined date.")
            .When(education => education.JoinedDate.HasValue && education.PassoutDate.HasValue);
    }
}
