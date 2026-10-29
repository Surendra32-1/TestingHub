using TeamHub.Application.DTOs.Permission;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Application.Validators.Permission;
using TeamHub.Application.Validators.Rbac;

namespace TeamHub.UnitTests.Validation;

public class PermissionAndRbacValidatorTests
{
    [Fact]
    public void PermissionCode_IsRequired()
    {
        var result = new CreateOrUpdatePermissionRequestValidator()
            .Validate(new CreateOrUpdatePermissionRequest { PermissionName = "Export users" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOrUpdatePermissionRequest.PermissionCode));
    }

    [Fact]
    public void PermissionCode_RejectsInvalidCharacters()
    {
        var result = new CreateOrUpdatePermissionRequestValidator()
            .Validate(new CreateOrUpdatePermissionRequest { PermissionCode = "users export!", PermissionName = "Export" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Permission_AcceptsWellFormedRequest()
    {
        var result = new CreateOrUpdatePermissionRequestValidator()
            .Validate(new CreateOrUpdatePermissionRequest
            {
                PermissionCode = "users.export",
                PermissionName = "Export users",
                Module = "Directory",
                IsActive = true
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SaveUserRoles_RejectsDuplicateRoleIds()
    {
        var result = new SaveUserRolesRequestValidator()
            .Validate(new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 1, RoleIds = [2, 2] }]
            });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SaveUserRoles_RejectsNonPositiveUserId()
    {
        var result = new SaveUserRolesRequestValidator()
            .Validate(new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 0, RoleIds = [1] }]
            });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SaveUserRoles_RejectsDuplicateUserEntries()
    {
        var result = new SaveUserRolesRequestValidator()
            .Validate(new SaveUserRolesRequest
            {
                Assignments =
                [
                    new UserRoleAssignmentDto { UserId = 1, RoleIds = [1] },
                    new UserRoleAssignmentDto { UserId = 1, RoleIds = [2] }
                ]
            });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SaveUserRoles_AcceptsRevokingAllRoles()
    {
        var result = new SaveUserRolesRequestValidator()
            .Validate(new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 1, RoleIds = [] }]
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SaveRolePermissions_RejectsDuplicateRoleIds()
    {
        var result = new SaveRolePermissionsRequestValidator()
            .Validate(new SaveRolePermissionsRequest
            {
                Assignments = [new RolePermissionAssignmentDto { PermissionId = 1, RoleIds = [3, 3] }]
            });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SaveRolePermissions_AcceptsValidBatch()
    {
        var result = new SaveRolePermissionsRequestValidator()
            .Validate(new SaveRolePermissionsRequest
            {
                Assignments =
                [
                    new RolePermissionAssignmentDto { PermissionId = 2, RoleIds = [1] },
                    new RolePermissionAssignmentDto { PermissionId = 3, RoleIds = [1, 2] }
                ]
            });

        Assert.True(result.IsValid);
    }
}
