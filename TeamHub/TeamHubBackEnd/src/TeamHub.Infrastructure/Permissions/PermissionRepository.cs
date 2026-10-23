using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Permission;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Permissions;

public sealed class PermissionRepository(IDbConnectionFactory connectionFactory) : IPermissionRepository
{
    public async Task<IReadOnlyList<GetPermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PermissionId, PermissionCode, PermissionName, Module, Description, IsActive
            FROM dbo.Permissions
            ORDER BY PermissionName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetPermissionDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<GetPermissionDto?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PermissionId, PermissionCode, PermissionName, Module, Description, IsActive
            FROM dbo.Permissions
            WHERE PermissionId = @PermissionId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetPermissionDto>(
            new CommandDefinition(sql, new { PermissionId = permissionId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(string permissionCode, int? excludePermissionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Permissions
            WHERE PermissionCode = @PermissionCode
              AND (@ExcludeId IS NULL OR PermissionId <> @ExcludeId);";
        using var connection = connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { PermissionCode = permissionCode, ExcludeId = excludePermissionId }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<int> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Permissions
                (PermissionCode, PermissionName, Module, Description, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.PermissionId
            VALUES
                (@PermissionCode, @PermissionName, @Module, @Description, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, permission, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Permissions
            SET PermissionCode = @PermissionCode,
                PermissionName = @PermissionName,
                Module = @Module,
                Description = @Description,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE PermissionId = @PermissionId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, permission, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int permissionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE dbo.Permissions SET IsActive = 0 WHERE PermissionId = @PermissionId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { PermissionId = permissionId }, cancellationToken: cancellationToken)) > 0;
    }
}
