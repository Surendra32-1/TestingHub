using TeamHub.Application.DTOs.JobType;

namespace TeamHub.Application.Contracts.Services;

public interface IJobTypeService
{
    Task<IReadOnlyList<GetJobTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetJobTypeDto?> GetByIdAsync(int jobTypeId, CancellationToken cancellationToken = default);
    Task<GetJobTypeDto> CreateAsync(CreateOrUpdateJobTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int jobTypeId, CreateOrUpdateJobTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int jobTypeId, CancellationToken cancellationToken = default);
}