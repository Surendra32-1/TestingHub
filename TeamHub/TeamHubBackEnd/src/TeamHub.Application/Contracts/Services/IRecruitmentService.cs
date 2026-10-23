using TeamHub.Application.DTOs.Recruitment;

namespace TeamHub.Application.Contracts.Services;

public interface IRecruitmentService
{
    Task<IReadOnlyList<GetRecruitmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetRecruitmentDto?> GetByIdAsync(int recruitmentId, CancellationToken cancellationToken = default);
    Task<GetRecruitmentDto> CreateAsync(CreateOrUpdateRecruitmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int recruitmentId, CreateOrUpdateRecruitmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int recruitmentId, CancellationToken cancellationToken = default);
}