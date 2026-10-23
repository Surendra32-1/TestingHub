using FluentValidation;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Validators.Employee;

public sealed class CreateOrUpdateEmployeeRequestValidator : AbstractValidator<CreateOrUpdateEmployeeRequest>
{
    public CreateOrUpdateEmployeeRequestValidator()
    {
        RuleFor(request => request.ENumber)
            .GreaterThan(0).WithMessage("Employee number must be greater than zero.");
        RuleFor(request => request.FirstName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must be 100 characters or fewer.");
        RuleFor(request => request.MiddleName)
            .MaximumLength(100).WithMessage("Middle name must be 100 characters or fewer.");
        RuleFor(request => request.LastName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must be 100 characters or fewer.");
        RuleFor(request => request.JoinedDate)
            .NotEqual(default(DateOnly)).WithMessage("Date of joining is required.");
        RuleFor(request => request.JoinedDateBS)
            .NotEmpty().WithMessage("B.S. date of joining is required.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. date of joining must use YYYY-MM-DD format.");
        RuleSet("Create", () => RuleFor(request => request.JoinedDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date of joining must be today or later."));
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(150).WithMessage("Email must be 150 characters or fewer.");
        RuleFor(request => request.AlternateEmail)
            .EmailAddress().WithMessage("Enter a valid alternate email address.")
            .MaximumLength(150).WithMessage("Alternate email must be 150 characters or fewer.")
            .When(request => !string.IsNullOrWhiteSpace(request.AlternateEmail));
        RuleFor(request => request.DesignationId).GreaterThan(0).WithMessage("Select a designation.");
        RuleFor(request => request.DepartmentId).GreaterThan(0).WithMessage("Select a department.");
        RuleFor(request => request.JobTypeId).GreaterThan(0).WithMessage("Select a job type.");
        RuleFor(request => request.BloodGroupId).GreaterThan(0).WithMessage("Select a blood group.");
        RuleFor(request => request.GenderId).GreaterThan(0).WithMessage("Select a gender.");
        RuleFor(request => request.ReportsToEmployeeId)
            .GreaterThan(0).WithMessage("Select a valid reporting employee.")
            .When(request => request.ReportsToEmployeeId.HasValue);
        RuleFor(request => request.LeadEmployeeId)
            .GreaterThan(0).WithMessage("Select a valid team lead.")
            .When(request => request.LeadEmployeeId.HasValue);
        RuleFor(request => request.RecruitmentId)
            .GreaterThan(0).WithMessage("Select a valid recruitment source.")
            .When(request => request.RecruitmentId.HasValue);
        RuleFor(request => request.ExtensionNumber)
            .GreaterThanOrEqualTo(0).WithMessage("Extension number cannot be negative.");
        RuleFor(request => request.CitizenshipNumber)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Citizenship number is required.")
            .MaximumLength(50).WithMessage("Citizenship number must be 50 characters or fewer.");
        RuleFor(request => request.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .When(request => request.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be before today.");
        RuleFor(request => request.DateOfBirthBS)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. date of birth must use YYYY-MM-DD format.")
            .When(request => !string.IsNullOrWhiteSpace(request.DateOfBirthBS));
        RuleFor(request => request.PhoneNumber).MaximumLength(20);
        RuleFor(request => request.MobileNumber).MaximumLength(20);
        RuleFor(request => request.EmergencyContactNumber).MaximumLength(20);
        RuleFor(request => request.EmergencyContactPerson)
            .NotEmpty().WithMessage("Emergency contact person is required.")
            .MaximumLength(150);
        RuleFor(request => request.FatherName)
            .NotEmpty().WithMessage("Father name is required.")
            .MaximumLength(150);
        RuleFor(request => request.MotherName).MaximumLength(150);
        RuleFor(request => request.SpouseName).MaximumLength(150);
        RuleFor(request => request.PANNumber)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("PAN number is required.")
            .MaximumLength(50).WithMessage("PAN number must be 50 characters or fewer.");
        RuleFor(request => request.StationNumber)
            .MaximumLength(20).WithMessage("Station number must be 20 characters or fewer.");
        RuleFor(request => request.LockerNumber)
            .MaximumLength(20).WithMessage("Locker number must be 20 characters or fewer.");
        RuleForEach(request => request.RoleIds)
            .GreaterThan(0).WithMessage("Select a valid role.");
        RuleFor(request => request.TeamId).GreaterThan(0).WithMessage("Select a team.");
        RuleFor(request => request.CotivityEmployeeId)
            .GreaterThan(0).WithMessage("Select a valid Cotivity employee.")
            .When(request => request.CotivityEmployeeId.HasValue);
        RuleForEach(request => request.Addresses).SetValidator(new EmployeeAddressDtoValidator());
        RuleForEach(request => request.Skills).SetValidator(new EmployeeSkillDtoValidator());
        RuleForEach(request => request.DressSizes)
            .ChildRules(dress => dress.RuleFor(item => item.DressId)
                .GreaterThan(0).WithMessage("Select a valid dress size."));
        RuleFor(request => request.DressSizes)
            .Must(dressSizes => dressSizes.Select(dress => dress.DressId).Distinct().Count() == dressSizes.Count)
            .WithMessage("A dress size can only be selected once.");
        RuleFor(request => request.Addresses)
            .Must(addresses => addresses.Count(address => address.IsPrimary) <= 1)
            .WithMessage("Only one primary address is allowed.");
        RuleFor(request => request.Skills)
            .Must(skills => skills.Count(skill => skill.IsPrimary) <= 1)
            .WithMessage("Only one primary skill is allowed.");
    }
}