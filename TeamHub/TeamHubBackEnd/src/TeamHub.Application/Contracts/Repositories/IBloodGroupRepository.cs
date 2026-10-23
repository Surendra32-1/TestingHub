using TeamHub.Application.DTOs.BloodGroup;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IBloodGroupRepository
{
    Task<IReadOnlyList<GetBloodGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetBloodGroupDto?> GetByIdAsync(int bloodGroupId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(BloodGroup bloodGroup, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(BloodGroup bloodGroup, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int bloodGroupId, CancellationToken cancellationToken = default);
}