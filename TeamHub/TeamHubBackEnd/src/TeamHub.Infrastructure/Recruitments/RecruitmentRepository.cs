using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Recruitments;

public sealed class RecruitmentRepository(IDbConnectionFactory connectionFactory) : IRecruitmentRepository
{
    public async Task<IReadOnlyList<GetRecruitmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT RecruitmentId, RecruitmentName, IsActive FROM Recruitments WHERE IsActive = 1 ORDER BY RecruitmentName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetRecruitmentDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetRecruitmentDto?> GetByIdAsync(int recruitmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT RecruitmentId, RecruitmentName, IsActive FROM Recruitments WHERE RecruitmentId = @RecruitmentId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetRecruitmentDto>(new CommandDefinition(sql, new { RecruitmentId = recruitmentId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(Recruitment recruitment, CancellationToken cancellationToken = default)
    {
        const string sql =@"INSERT INTO Recruitments (RecruitmentName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.RecruitmentId VALUES (@RecruitmentName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, recruitment, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(Recruitment recruitment, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Recruitments SET RecruitmentName = @RecruitmentName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE RecruitmentId = @RecruitmentId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, recruitment, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int recruitmentId, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE Recruitments SET IsActive = 0 WHERE RecruitmentId = @RecruitmentId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { RecruitmentId = recruitmentId }, cancellationToken: cancellationToken)) > 0;
    }
}