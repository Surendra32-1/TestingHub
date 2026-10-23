using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Skill;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Skills;

public sealed class SkillRepository(IDbConnectionFactory connectionFactory) : ISkillRepository
{
    public async Task<IReadOnlyList<GetSkillDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT SkillId, SkillName, IsActive FROM Skills WHERE IsActive = 1 ORDER BY SkillName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetSkillDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetSkillDto?> GetByIdAsync(int skillId, CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT SkillId, SkillName, IsActive FROM Skills WHERE SkillId = @SkillId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetSkillDto>(new CommandDefinition(sql, new { SkillId = skillId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        const string sql =@"INSERT INTO Skills (SkillName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.SkillId VALUES (@SkillName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, skill, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Skills SET SkillName = @SkillName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE SkillId = @SkillId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, skill, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int skillId, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Skills SET IsActive = 0 WHERE SkillId = @SkillId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { SkillId = skillId }, cancellationToken: cancellationToken)) > 0;
    }
}