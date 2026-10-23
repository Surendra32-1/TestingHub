using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Department;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Departments;

public sealed class DepartmentRepository(IDbConnectionFactory connectionFactory) : IDepartmentRepository
{
    public async Task<IReadOnlyList<GetDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT DepartmentId, DepartmentName, IsActive
            FROM Departments
            ORDER BY DepartmentName;
            ";

        using var connection = connectionFactory.CreateConnection();
        var departments = await connection.QueryAsync<GetDepartmentDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return departments.AsList();
    }

    public async Task<GetDepartmentDto?> GetByIdAsync( int departmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT DepartmentId, DepartmentName, IsActive
            FROM Departments
            WHERE DepartmentId = @DepartmentId;
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetDepartmentDto>(
            new CommandDefinition(sql, new { DepartmentId = departmentId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(Department department, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO Departments (DepartmentName, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.DepartmentId
            VALUES (@DepartmentName, @IsActive, @CreatedDate, @CreatedBy);
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, department, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE Departments
            SET DepartmentName = @DepartmentName,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE DepartmentId = @DepartmentId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, department, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE Departments
            SET IsActive = 0
            WHERE DepartmentId = @DepartmentId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { DepartmentId = departmentId }, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }
}