using Dapper;
using Microsoft.Data.SqlClient;
using TeamHub.Application.Common;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;
using TeamHub.Infrastructure.Permissions;
using TeamHub.Infrastructure.RolePermissions;
using TeamHub.Infrastructure.UserRoles;

namespace TeamHub.IntegrationTests;

public class RbacRepositoryTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("TEAMHUB_TEST_CONNECTION_STRING");

    [Fact]
    public async Task EmployeeRoleMatrix_LoadsWithoutError()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var repository = new UserRoleRepository(new SqlConnectionFactory(connectionString));
        var matrix = await repository.GetMatrixAsync(1, 100, null);

        Assert.NotNull(matrix);
        Assert.NotNull(matrix.Roles);
        Assert.NotNull(matrix.Rows);
    }

    [Fact]
    public async Task RolePermissionMatrix_LoadsWithoutError()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var repository = new RolePermissionRepository(new SqlConnectionFactory(connectionString));
        var matrix = await repository.GetMatrixAsync(1, 100, null);

        Assert.NotNull(matrix);
        Assert.NotNull(matrix.Roles);
        Assert.NotNull(matrix.Rows);
    }

    [Fact]
    public async Task UserRoleSaveAsync_RejectsUnknownUserAndCommitsNothing()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var repository = new UserRoleRepository(new SqlConnectionFactory(connectionString));
        var assignments = new List<UserRoleAssignmentDto>
        {
            new() { UserId = int.MaxValue, RoleIds = [] }
        };

        await Assert.ThrowsAsync<RbacValidationException>(() => repository.SaveAsync(assignments, "IntegrationTest"));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.UserRoles WHERE UserId = @UserId;", new { UserId = int.MaxValue });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task PermissionRepository_CreateReadUpdateSoftDelete_RoundTrips()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var code = $"it.test.{Guid.NewGuid():N}".Substring(0, 40);
        var repository = new PermissionRepository(new SqlConnectionFactory(connectionString));
        try
        {
            var created = await repository.CreateAsync(new Permission
            {
                PermissionCode = code,
                PermissionName = "Integration test permission",
                Module = "Test",
                Description = "Created by RBAC integration test",
                IsActive = true,
                CreatedBy = "IntegrationTest",
                CreatedDate = DateTime.UtcNow
            });
            Assert.True(created > 0);

            Assert.True(await repository.CodeExistsAsync(code, null));
            Assert.False(await repository.CodeExistsAsync(code, created));

            var fetched = await repository.GetByIdAsync(created);
            Assert.NotNull(fetched);
            Assert.Equal(code, fetched.PermissionCode);

            Assert.True(await repository.UpdateAsync(new Permission
            {
                PermissionId = created,
                PermissionCode = code,
                PermissionName = "Updated by RBAC integration test",
                Module = "Test",
                Description = null,
                IsActive = true,
                LastModifiedBy = "IntegrationTest",
                LastModifiedDate = DateTime.UtcNow
            }));

            Assert.True(await repository.DeleteAsync(created));
            Assert.Null(await repository.GetByIdAsync(created));
        }
        finally
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                "DELETE FROM dbo.RolePermissions WHERE PermissionId IN (SELECT PermissionId FROM dbo.Permissions WHERE PermissionCode = @Code);",
                new { Code = code });
            await connection.ExecuteAsync(
                "DELETE FROM dbo.Permissions WHERE PermissionCode = @Code;", new { Code = code });
        }
    }
}
