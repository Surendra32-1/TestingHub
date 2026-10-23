using TeamHub.Application.DTOs.Team;

namespace TeamHub.Application.Contracts.Services;

public interface ITeamService
{
    Task<IReadOnlyList<GetTeamDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetTeamDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken = default);
    Task<GetTeamDto> CreateAsync(CreateOrUpdateTeamRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int teamId, CreateOrUpdateTeamRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int teamId, CancellationToken cancellationToken = default);
}