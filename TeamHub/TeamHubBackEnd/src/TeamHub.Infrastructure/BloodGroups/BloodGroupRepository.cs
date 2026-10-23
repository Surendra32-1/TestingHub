using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.BloodGroup;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.BloodGroups;

public sealed class BloodGroupRepository(IDbConnectionFactory connectionFactory) : IBloodGroupRepository
{
    public async Task<IReadOnlyList<GetBloodGroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT BloodGroupId, BloodGroupName, IsActive
            FROM BloodGroups
            ORDER BY BloodGroupName;
            ";

        using var connection = connectionFactory.CreateConnection();
        var bloodGroups = await connection.QueryAsync<GetBloodGroupDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return bloodGroups.AsList();
    }

    public async Task<GetBloodGroupDto?> GetByIdAsync(int bloodGroupId,CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT BloodGroupId, BloodGroupName, IsActive
            FROM BloodGroups
            WHERE BloodGroupId = @BloodGroupId;
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetBloodGroupDto>(
            new CommandDefinition(sql, new { BloodGroupId = bloodGroupId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(BloodGroup bloodGroup, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO BloodGroups (BloodGroupName, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.BloodGroupId
            VALUES (@BloodGroupName, @IsActive, @CreatedDate, @CreatedBy);
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, bloodGroup, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(BloodGroup bloodGroup, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE BloodGroups
            SET BloodGroupName = @BloodGroupName,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE BloodGroupId = @BloodGroupId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, bloodGroup, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int bloodGroupId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE BloodGroups
            SET IsActive = 0
            WHERE BloodGroupId = @BloodGroupId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { BloodGroupId = bloodGroupId }, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }
}