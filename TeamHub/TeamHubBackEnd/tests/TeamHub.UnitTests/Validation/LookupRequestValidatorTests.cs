using FluentValidation;
using TeamHub.Application.DTOs.AddressType;
using TeamHub.Application.DTOs.BloodGroup;
using TeamHub.Application.DTOs.Department;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Application.DTOs.Gender;
using TeamHub.Application.DTOs.JobType;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Application.DTOs.Role;
using TeamHub.Application.DTOs.Skill;
using TeamHub.Application.DTOs.Team;
using TeamHub.Application.Validators.AddressType;
using TeamHub.Application.Validators.BloodGroup;
using TeamHub.Application.Validators.Department;
using TeamHub.Application.Validators.Designation;
using TeamHub.Application.Validators.Gender;
using TeamHub.Application.Validators.JobType;
using TeamHub.Application.Validators.Recruitment;
using TeamHub.Application.Validators.Role;
using TeamHub.Application.Validators.Skill;
using TeamHub.Application.Validators.Team;

namespace TeamHub.UnitTests.Validation;

public class LookupRequestValidatorTests
{
    [Fact]
    public void DepartmentName_IsRequired()
    {
        var result = new CreateOrUpdateDepartmentRequestValidator()
            .Validate(new CreateOrUpdateDepartmentRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOrUpdateDepartmentRequest.DepartmentName));
    }

    [Fact]
    public void DesignationRank_CannotBeNegative()
    {
        var result = new CreateOrUpdateDesignationRequestValidator()
            .Validate(new CreateOrUpdateDesignationRequest
            {
                DesignationName = "Developer",
                Rank = -1
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOrUpdateDesignationRequest.Rank));
    }

    [Fact]
    public void BloodGroupName_CannotExceedDatabaseLength()
    {
        var result = new CreateOrUpdateBloodGroupRequestValidator()
            .Validate(new CreateOrUpdateBloodGroupRequest
            {
                BloodGroupName = "ABPLUS"
            });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Gender", "GenderName")]
    [InlineData("AddressType", "AddressTypeName")]
    [InlineData("Role", "RoleName")]
    [InlineData("Team", "TeamName")]
    [InlineData("Skill", "SkillName")]
    [InlineData("Recruitment", "RecruitmentName")]
    [InlineData("JobType", "JobTypeName")]
    public void LookupNames_AreRequired(string lookup, string propertyName)
    {
        var isValid = lookup switch
        {
            "Gender" => new CreateOrUpdateGenderRequestValidator().Validate(new CreateOrUpdateGenderRequest()).IsValid,
            "AddressType" => new CreateOrUpdateAddressTypeRequestValidator().Validate(new CreateOrUpdateAddressTypeRequest()).IsValid,
            "Role" => new CreateOrUpdateRoleRequestValidator().Validate(new CreateOrUpdateRoleRequest()).IsValid,
            "Team" => new CreateOrUpdateTeamRequestValidator().Validate(new CreateOrUpdateTeamRequest()).IsValid,
            "Skill" => new CreateOrUpdateSkillRequestValidator().Validate(new CreateOrUpdateSkillRequest()).IsValid,
            "Recruitment" => new CreateOrUpdateRecruitmentRequestValidator().Validate(new CreateOrUpdateRecruitmentRequest()).IsValid,
            "JobType" => new CreateOrUpdateJobTypeRequestValidator().Validate(new CreateOrUpdateJobTypeRequest()).IsValid,
            _ => throw new ArgumentOutOfRangeException(nameof(lookup))
        };

        Assert.False(isValid, $"{lookup}.{propertyName} should be required.");
    }
}