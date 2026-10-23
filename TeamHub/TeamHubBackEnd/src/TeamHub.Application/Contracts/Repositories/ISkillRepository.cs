using TeamHub.Application.DTOs.Skill;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface ISkillRepository
{
    Task<IReadOnlyList<GetSkillDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetSkillDto?> GetByIdAsync(int skillId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Skill skill, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Skill skill, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int skillId, CancellationToken cancellationToken = default);
}