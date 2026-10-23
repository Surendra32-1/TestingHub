using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.CotivityDepartment;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.CotivityDepartments;

public sealed class CotivityDepartmentRepository(IDbConnectionFactory connectionFactory) : ICotivityDepartmentRepository
{
    public async Task<IReadOnlyList<GetCotivityDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT CotivityDepartmentId, CotivityDepartmentName, IsActive
            FROM CotivityDepartments
            ORDER BY CotivityDepartmentName;
            ";
        using var connection = connectionFactory.CreateConnection();
        var departments = await connection.QueryAsync<GetCotivityDepartmentDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return departments.AsList();
    }

    public async Task<GetCotivityDepartmentDto?> GetByIdAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT CotivityDepartmentId, CotivityDepartmentName, IsActive
            FROM CotivityDepartments
            WHERE CotivityDepartmentId = @CotivityDepartmentId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetCotivityDepartmentDto>(
            new CommandDefinition(sql, new { CotivityDepartmentId = cotivityDepartmentId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(CotivityDepartment department, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO CotivityDepartments (CotivityDepartmentName, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.CotivityDepartmentId
            VALUES (@CotivityDepartmentName, @IsActive, @CreatedDate, @CreatedBy);
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, department, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(CotivityDepartment department, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityDepartments
            SET CotivityDepartmentName = @CotivityDepartmentName,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE CotivityDepartmentId = @CotivityDepartmentId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, department, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityDepartments SET IsActive = 0
            WHERE CotivityDepartmentId = @CotivityDepartmentId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { CotivityDepartmentId = cotivityDepartmentId }, cancellationToken: cancellationToken)) > 0;
    }
}