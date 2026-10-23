using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.DressSize;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.DressSizes;

public sealed class DressSizeRepository(IDbConnectionFactory connectionFactory) : IDressSizeRepository
{
    public async Task<IReadOnlyList<GetDressSizeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT DressId, DressName, Size, IsActive FROM DressSizes ORDER BY DressName, Size;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<GetDressSizeDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<GetDressSizeDto?> GetByIdAsync(int dressId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT DressId, DressName, Size, IsActive FROM DressSizes WHERE DressId = @DressId;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetDressSizeDto>(new CommandDefinition(
            sql, new { DressId = dressId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(DressSize dressSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO DressSizes (DressName, Size, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.DressId
            VALUES (@DressName, @Size, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, dressSize, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(DressSize dressSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE DressSizes
            SET DressName = @DressName,
                Size = @Size,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE DressId = @DressId;";
        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(sql, dressSize, cancellationToken: cancellationToken));
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int dressId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE DressSizes
            SET IsActive = 0
            WHERE DressId = @DressId;";
        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { DressId = dressId }, cancellationToken: cancellationToken));
        return affectedRows > 0;
    }
}