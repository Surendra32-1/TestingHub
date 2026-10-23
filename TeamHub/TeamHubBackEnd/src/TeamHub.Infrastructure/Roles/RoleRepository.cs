using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Role;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Roles;

public sealed class RoleRepository(IDbConnectionFactory connectionFactory) : IRoleRepository
{
    public async Task<IReadOnlyList<GetRoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT RoleId, RoleName, IsActive FROM Roles WHERE IsActive = 1 ORDER BY RoleName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetRoleDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetRoleDto?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT RoleId, RoleName, IsActive FROM Roles WHERE RoleId = @RoleId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetRoleDto>(new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string sql =@"INSERT INTO Roles (RoleName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.RoleId VALUES (@RoleName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, role, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Roles SET RoleName = @RoleName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE RoleId = @RoleId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, role, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int roleId, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Roles SET IsActive = 0 WHERE RoleId = @RoleId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken)) > 0;
    }
}