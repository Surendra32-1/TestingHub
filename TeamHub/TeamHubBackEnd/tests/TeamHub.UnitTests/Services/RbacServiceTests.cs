using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Permission;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Application.DTOs.Role;
using TeamHub.Application.Services;
using TeamHub.Domain.Entities;

namespace TeamHub.UnitTests.Services;

public class RbacServiceTests
{
    [Fact]
    public async Task UserRoleMatrix_SuperAdmin_PersistsWithoutHoldingRoles()
    {
        var repository = new FakeUserRoleRepository();
        var roleRepository = new FakeRoleRepository([Role(1, "Admin"), Role(2, "Viewer")]);
        var service = new UserRoleMatrixService(repository, roleRepository);

        await service.SaveAsync(
            new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 10, RoleIds = [1, 2] }]
            },
            actingRoleNames: Array.Empty<string>(),
            isSuperAdmin: true);

        Assert.True(repository.SaveCalled);
        Assert.Single(repository.LastAssignments);
        Assert.Equal(new[] { 1, 2 }, repository.LastAssignments[0].RoleIds);
    }

    [Fact]
    public async Task UserRoleMatrix_NonSuperAdminCannotGrantRoleTheyDoNotHold()
    {
        var repository = new FakeUserRoleRepository();
        var roleRepository = new FakeRoleRepository([Role(1, "Admin"), Role(2, "Viewer")]);
        var service = new UserRoleMatrixService(repository, roleRepository);

        var ex = await Assert.ThrowsAsync<PrivilegeEscalationException>(() => service.SaveAsync(
            new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 10, RoleIds = [1] }]
            },
            actingRoleNames: new[] { "Viewer" },
            isSuperAdmin: false));

        Assert.Contains("Admin", ex.Message);
        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task UserRoleMatrix_RejectsUnknownOrInactiveRoleId()
    {
        var repository = new FakeUserRoleRepository();
        var roleRepository = new FakeRoleRepository([Role(2, "Viewer")]);
        var service = new UserRoleMatrixService(repository, roleRepository);

        await Assert.ThrowsAsync<RbacValidationException>(() => service.SaveAsync(
            new SaveUserRolesRequest
            {
                Assignments = [new UserRoleAssignmentDto { UserId = 10, RoleIds = [999] }]
            },
            actingRoleNames: new[] { "Viewer" },
            isSuperAdmin: false));

        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task UserRoleMatrix_EmptyBatchIsNoOp()
    {
        var repository = new FakeUserRoleRepository();
        var roleRepository = new FakeRoleRepository([Role(1, "Admin")]);
        var service = new UserRoleMatrixService(repository, roleRepository);

        await service.SaveAsync(new SaveUserRolesRequest(), actingRoleNames: ["Viewer"], isSuperAdmin: false);

        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task RolePermissionMatrix_NonSuperAdminCannotGrantPermissionTheyDoNotHold()
    {
        var repository = new FakeRolePermissionRepository();
        var permissionRepository = new FakePermissionRepository(
            [PermissionDto(1, "roles.view"), PermissionDto(2, "roles.manage")]);
        var service = new RolePermissionMatrixService(repository, permissionRepository);

        await Assert.ThrowsAsync<PrivilegeEscalationException>(() => service.SaveAsync(
            new SaveRolePermissionsRequest
            {
                Assignments = [new RolePermissionAssignmentDto { RoleId = 5, PermissionIds = [2] }]
            },
            actingPermissionCodes: new[] { "roles.view" },
            isSuperAdmin: false));

        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task RolePermissionMatrix_SuperAdminPersistsRegardlessOfHeldCodes()
    {
        var repository = new FakeRolePermissionRepository();
        var permissionRepository = new FakePermissionRepository([PermissionDto(2, "roles.manage")]);
        var service = new RolePermissionMatrixService(repository, permissionRepository);

        await service.SaveAsync(
            new SaveRolePermissionsRequest
            {
                Assignments = [new RolePermissionAssignmentDto { RoleId = 5, PermissionIds = [2] }]
            },
            actingPermissionCodes: Array.Empty<string>(),
            isSuperAdmin: true);

        Assert.True(repository.SaveCalled);
    }

    [Fact]
    public async Task PermissionService_CreateAsync_TrimsFieldsAndStampsSystemActor()
    {
        var repository = new FakePermissionRepository();
        var service = new PermissionService(repository);

        var result = await service.CreateAsync(new CreateOrUpdatePermissionRequest
        {
            PermissionCode = "  users.export  ",
            PermissionName = "  Export users  ",
            Module = "   ",
            Description = "  Bulk export  ",
            IsActive = true
        });

        Assert.Equal(7, result.PermissionId);
        Assert.Equal("users.export", result.PermissionCode);
        Assert.Equal("Export users", result.PermissionName);
        Assert.Null(result.Module);
        Assert.Equal("Bulk export", result.Description);
        Assert.Equal("System", repository.CreatedPermission?.CreatedBy);
    }

    [Fact]
    public async Task PermissionService_CreateAsync_RejectsDuplicateCode()
    {
        var repository = new FakePermissionRepository(codeExists: true);
        var service = new PermissionService(repository);

        await Assert.ThrowsAsync<RbacValidationException>(() => service.CreateAsync(
            new CreateOrUpdatePermissionRequest { PermissionCode = "users.export", PermissionName = "Export" }));

        Assert.Null(repository.CreatedPermission);
    }

    private static GetRoleDto Role(int id, string name) => new() { RoleId = id, RoleName = name, IsActive = true };

    private static GetPermissionDto PermissionDto(int id, string code) =>
        new() { PermissionId = id, PermissionCode = code, PermissionName = code, IsActive = true };

    private sealed class FakeUserRoleRepository : IUserRoleRepository
    {
        public bool SaveCalled { get; private set; }
        public IReadOnlyList<UserRoleAssignmentDto> LastAssignments { get; private set; } = [];

        public Task<EmployeeRoleMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmployeeRoleMatrixDto());

        public Task SaveAsync(IReadOnlyList<UserRoleAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            LastAssignments = assignments;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRolePermissionRepository : IRolePermissionRepository
    {
        public bool SaveCalled { get; private set; }

        public Task<RolePermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RolePermissionMatrixDto());

        public Task SaveAsync(IReadOnlyList<RolePermissionAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRoleRepository(params GetRoleDto[] roles) : IRoleRepository
    {
        public Task<IReadOnlyList<GetRoleDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GetRoleDto>>(roles);

        public Task<GetRoleDto?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetRoleDto?>(roles.FirstOrDefault(role => role.RoleId == roleId));

        public Task<int> CreateAsync(Role role, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int roleId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class FakePermissionRepository(
        IReadOnlyList<GetPermissionDto>? all = null,
        bool codeExists = false) : IPermissionRepository
    {
        public Permission? CreatedPermission { get; private set; }

        public Task<IReadOnlyList<GetPermissionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(all ?? Array.Empty<GetPermissionDto>());

        public Task<GetPermissionDto?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(all?.FirstOrDefault(permission => permission.PermissionId == permissionId));

        public Task<bool> CodeExistsAsync(string permissionCode, int? excludePermissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(codeExists);

        public Task<int> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            CreatedPermission = permission;
            return Task.FromResult(7);
        }

        public Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int permissionId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
