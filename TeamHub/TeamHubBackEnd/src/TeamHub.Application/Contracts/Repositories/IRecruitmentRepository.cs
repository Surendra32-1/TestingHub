using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IRecruitmentRepository
{
    Task<IReadOnlyList<GetRecruitmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetRecruitmentDto?> GetByIdAsync(int recruitmentId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Recruitment recruitment, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Recruitment recruitment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int recruitmentId, CancellationToken cancellationToken = default);
}