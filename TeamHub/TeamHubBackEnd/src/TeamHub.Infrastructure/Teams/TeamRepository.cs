using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Team;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.Teams;

public sealed class TeamRepository(IDbConnectionFactory connectionFactory) : ITeamRepository
{
    public async Task<IReadOnlyList<GetTeamDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT TeamId, TeamName, IsActive FROM Teams WHERE IsActive = 1 ORDER BY TeamName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetTeamDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetTeamDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT TeamId, TeamName, IsActive FROM Teams WHERE TeamId = @TeamId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetTeamDto>(new CommandDefinition(sql, new { TeamId = teamId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(Team team, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO Teams (TeamName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.TeamId VALUES (@TeamName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, team, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Teams SET TeamName = @TeamName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE TeamId = @TeamId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, team, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int teamId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Teams SET IsActive = 0 WHERE TeamId = @TeamId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { TeamId = teamId }, cancellationToken: cancellationToken)) > 0;
    }
}