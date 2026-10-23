using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Designations;

public sealed class DesignationRepository(IDbConnectionFactory connectionFactory) : IDesignationRepository
{
    public async Task<IReadOnlyList<GetDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT DesignationId, DesignationName, [Rank], IsActive
            FROM Designations
            ORDER BY [Rank], DesignationName;
            ";

        using var connection = connectionFactory.CreateConnection();
        var designations = await connection.QueryAsync<GetDesignationDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return designations.AsList();
    }

    public async Task<GetDesignationDto?> GetByIdAsync(int designationId,CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT DesignationId, DesignationName, [Rank], IsActive
            FROM Designations
            WHERE DesignationId = @DesignationId;
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetDesignationDto>(
            new CommandDefinition(sql, new { DesignationId = designationId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(Designation designation, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO Designations
                (DesignationName, [Rank], IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.DesignationId
            VALUES (@DesignationName, @Rank, @IsActive, @CreatedDate, @CreatedBy);
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, designation, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Designation designation, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE Designations
            SET DesignationName = @DesignationName,
                [Rank] = @Rank,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE DesignationId = @DesignationId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, designation, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int designationId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE Designations
            SET IsActive = 0
            WHERE DesignationId = @DesignationId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { DesignationId = designationId }, cancellationToken: cancellationToken));

        return affectedRows > 0;
    }
}