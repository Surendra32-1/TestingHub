using TeamHub.Application.DTOs.JobType;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IJobTypeRepository
{
    Task<IReadOnlyList<GetJobTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetJobTypeDto?> GetByIdAsync(int jobTypeId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(JobType jobType, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(JobType jobType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int jobTypeId, CancellationToken cancellationToken = default);
}