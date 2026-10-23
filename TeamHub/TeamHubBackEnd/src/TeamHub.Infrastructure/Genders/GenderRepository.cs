using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Gender;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Genders;

public sealed class GenderRepository(IDbConnectionFactory connectionFactory) : IGenderRepository
{
    public async Task<IReadOnlyList<GetGenderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT GenderId, GenderName, IsActive FROM Gender WHERE IsActive = 1 ORDER BY GenderName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetGenderDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetGenderDto?> GetByIdAsync(int genderId, CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT GenderId, GenderName, IsActive FROM Gender WHERE GenderId = @GenderId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetGenderDto>(new CommandDefinition(sql, new { GenderId = genderId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(Gender gender, CancellationToken cancellationToken = default)
    {
        const string sql =@"INSERT INTO Gender (GenderName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.GenderId VALUES (@GenderName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, gender, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(Gender gender, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Gender SET GenderName = @GenderName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE GenderId = @GenderId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, gender, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int genderId, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Gender SET IsActive = 0 WHERE GenderId = @GenderId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { GenderId = genderId }, cancellationToken: cancellationToken)) > 0;
    }
}