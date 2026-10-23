using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.CotivityDesignation;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.CotivityDesignations;

public sealed class CotivityDesignationRepository(IDbConnectionFactory connectionFactory) : ICotivityDesignationRepository
{
    public async Task<IReadOnlyList<GetCotivityDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT CotivityDesignationId, CotivityDesignationName, CotivityRank, IsActive
            FROM CotivityDesignations
            ORDER BY CotivityRank, CotivityDesignationName;
            ";
        using var connection = connectionFactory.CreateConnection();
        var designations = await connection.QueryAsync<GetCotivityDesignationDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return designations.AsList();
    }

    public async Task<GetCotivityDesignationDto?> GetByIdAsync(int cotivityDesignationId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT CotivityDesignationId, CotivityDesignationName, CotivityRank, IsActive
            FROM CotivityDesignations
            WHERE CotivityDesignationId = @CotivityDesignationId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetCotivityDesignationDto>(
            new CommandDefinition(sql, new { CotivityDesignationId = cotivityDesignationId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(CotivityDesignation designation, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO CotivityDesignations (CotivityDesignationName, CotivityRank, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.CotivityDesignationId
            VALUES (@CotivityDesignationName, @CotivityRank, @IsActive, @CreatedDate, @CreatedBy);
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, designation, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(CotivityDesignation designation, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityDesignations
            SET CotivityDesignationName = @CotivityDesignationName,
                CotivityRank = @CotivityRank,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE CotivityDesignationId = @CotivityDesignationId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, designation, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int cotivityDesignationId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityDesignations SET IsActive = 0
            WHERE CotivityDesignationId = @CotivityDesignationId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { CotivityDesignationId = cotivityDesignationId }, cancellationToken: cancellationToken)) > 0;
    }
}