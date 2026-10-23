using TeamHub.Application.DTOs.Team;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface ITeamRepository
{
    Task<IReadOnlyList<GetTeamDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetTeamDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Team team, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Team team, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int teamId, CancellationToken cancellationToken = default);
}