using TeamHub.Application.DTOs.Skill;

namespace TeamHub.Application.Contracts.Services;

public interface ISkillService
{
    Task<IReadOnlyList<GetSkillDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetSkillDto?> GetByIdAsync(int skillId, CancellationToken cancellationToken = default);
    Task<GetSkillDto> CreateAsync(CreateOrUpdateSkillRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int skillId, CreateOrUpdateSkillRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int skillId, CancellationToken cancellationToken = default);
}