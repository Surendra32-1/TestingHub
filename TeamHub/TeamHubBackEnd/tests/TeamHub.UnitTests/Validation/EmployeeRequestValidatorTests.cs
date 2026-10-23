using TeamHub.Application.DTOs.Employee;
using TeamHub.Application.Validators.Employee;
using FluentValidation;
using FluentValidation.Internal;
using System.ComponentModel.DataAnnotations;
using TeamHub.Application.DTOs.EmployeeRelease;

namespace TeamHub.UnitTests.Validation;

public class EmployeeRequestValidatorTests
{
    [Fact]
    public void EmployeeEmail_UsesDatabaseColumnLength()
    {
        var request = new CreateOrUpdateEmployeeRequest
        {
            ENumber = 1,
            FirstName = "Alex",
            LastName = "Smith",
            JoinedDate = new DateOnly(2024, 1, 1),
            Email = $"{new string('a', 140)}@example.com",
            ExtensionNumber = 0,
            CitizenshipNumber = "123",
            PANNumber = "PAN",
            DesignationId = 1,
            DepartmentId = 1,
            JobTypeId = 1,
            BloodGroupId = 1,
            GenderId = 1,
            RoleId = 1,
            TeamId = 1
        };

        var result = new CreateOrUpdateEmployeeRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void ReleaseRecord_ValidatesReleaseTypeAndStatus()
    {
        var request = new EmployeeReleaseRequest { ReleaseType = "Invalid", Status = "Invalid" };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.ReleaseType)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.Status)));
    }

    [Fact]
    public void EmployeeDateOfBirth_MustBeBeforeToday()
    {
        var request = new CreateOrUpdateEmployeeRequest
        {
            ENumber = 1,
            FirstName = "Alex",
            LastName = "Smith",
            JoinedDate = new DateOnly(2024, 1, 1),
            Email = "alex@example.com",
            ExtensionNumber = 0,
            CitizenshipNumber = "123",
            PANNumber = "PAN",
            DesignationId = 1,
            DepartmentId = 1,
            JobTypeId = 1,
            BloodGroupId = 1,
            GenderId = 1,
            RoleId = 1,
            TeamId = 1,
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today)
        };

        var result = new CreateOrUpdateEmployeeRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.DateOfBirth));
    }

    [Fact]
    public void EmployeeJoinedDate_CannotBeBeforeToday()
    {
        var request = new CreateOrUpdateEmployeeRequest
        {
            ENumber = 1,
            FirstName = "Alex",
            LastName = "Smith",
            JoinedDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            Email = "alex@example.com",
            ExtensionNumber = 0,
            CitizenshipNumber = "123",
            PANNumber = "PAN",
            DesignationId = 1,
            DepartmentId = 1,
            JobTypeId = 1,
            BloodGroupId = 1,
            GenderId = 1,
            RoleId = 1,
            TeamId = 1
        };

        var context = new ValidationContext<CreateOrUpdateEmployeeRequest>(
            request,
            new PropertyChain(),
            new RulesetValidatorSelector(new[] { "default", "Create" }));
        var result = new CreateOrUpdateEmployeeRequestValidator().Validate(context);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.JoinedDate));
    }

    [Fact]
    public void EmployeeJoinedDate_MayBeHistoricalDuringUpdate()
    {
        var request = new CreateOrUpdateEmployeeRequest
        {
            ENumber = 1,
            FirstName = "Alex",
            LastName = "Smith",
            JoinedDate = new DateOnly(2024, 1, 1),
            Email = "alex@example.com",
            ExtensionNumber = 0,
            CitizenshipNumber = "123",
            PANNumber = "PAN",
            DesignationId = 1,
            DepartmentId = 1,
            JobTypeId = 1,
            BloodGroupId = 1,
            GenderId = 1,
            RoleId = 1,
            TeamId = 1
        };

        var result = new CreateOrUpdateEmployeeRequestValidator().Validate(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(request.JoinedDate));
    }
}