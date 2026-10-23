using TeamHub.Application.DTOs.BloodGroup;

namespace TeamHub.Application.Contracts.Services;

public interface IBloodGroupService
{
    Task<IReadOnlyList<GetBloodGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetBloodGroupDto?> GetByIdAsync(int bloodGroupId, CancellationToken cancellationToken = default);

    Task<GetBloodGroupDto> CreateAsync(CreateOrUpdateBloodGroupRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int bloodGroupId, CreateOrUpdateBloodGroupRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int bloodGroupId, CancellationToken cancellationToken = default);
}