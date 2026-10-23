using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.JobType;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.JobTypes;

public sealed class JobTypeRepository(IDbConnectionFactory connectionFactory) : IJobTypeRepository
{
    public async Task<IReadOnlyList<GetJobTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT JobTypeId, JobTypeName, IsActive FROM JobTypes WHERE IsActive = 1 ORDER BY JobTypeName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetJobTypeDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetJobTypeDto?> GetByIdAsync(int jobTypeId, CancellationToken cancellationToken = default)
    {
        const string sql =@"SELECT JobTypeId, JobTypeName, IsActive FROM JobTypes WHERE JobTypeId = @JobTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetJobTypeDto>(new CommandDefinition(sql, new { JobTypeId = jobTypeId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(JobType jobType, CancellationToken cancellationToken = default)
    {
        const string sql =@"INSERT INTO JobTypes (JobTypeName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.JobTypeId VALUES (@JobTypeName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, jobType, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(JobType jobType, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE JobTypes SET JobTypeName = @JobTypeName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE JobTypeId = @JobTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, jobType, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int jobTypeId, CancellationToken cancellationToken = default)
    {
        const string sql =@"UPDATE JobTypes SET IsActive = 0 WHERE JobTypeId = @JobTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { JobTypeId = jobTypeId }, cancellationToken: cancellationToken)) > 0;
    }
}