using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.BusinessUnit;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.BusinessUnits;

public sealed class BusinessUnitRepository(IDbConnectionFactory connectionFactory) : IBusinessUnitRepository
{
    public async Task<IReadOnlyList<GetBusinessUnitDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT BusinessUnitId, BusinessUnitName, COALESCE(IsActive, 1) AS IsActive FROM BusinessUnits ORDER BY BusinessUnitName;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<GetBusinessUnitDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<GetBusinessUnitDto?> GetByIdAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT BusinessUnitId, BusinessUnitName, COALESCE(IsActive, 1) AS IsActive FROM BusinessUnits WHERE BusinessUnitId = @BusinessUnitId;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetBusinessUnitDto>(new CommandDefinition(
            sql, new { BusinessUnitId = businessUnitId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(BusinessUnit businessUnit, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO BusinessUnits (BusinessUnitName, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.BusinessUnitId
            VALUES (@BusinessUnitName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, businessUnit, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(BusinessUnit businessUnit, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE BusinessUnits
            SET BusinessUnitName = @BusinessUnitName,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE BusinessUnitId = @BusinessUnitId;";
        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, businessUnit, cancellationToken: cancellationToken));
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE BusinessUnits
            SET IsActive = 0
            WHERE BusinessUnitId = @BusinessUnitId;";
        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { BusinessUnitId = businessUnitId }, cancellationToken: cancellationToken));
        return affectedRows > 0;
    }
}