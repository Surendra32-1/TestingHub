using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.BloodGroup;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class BloodGroupService(IBloodGroupRepository repository) : IBloodGroupService
{
    public Task<IReadOnlyList<GetBloodGroupDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetBloodGroupDto?> GetByIdAsync(int bloodGroupId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(bloodGroupId, cancellationToken);

    public async Task<GetBloodGroupDto> CreateAsync(CreateOrUpdateBloodGroupRequest request,CancellationToken cancellationToken = default)
    {
        var bloodGroup = new BloodGroup
        {
            BloodGroupName = request.BloodGroupName,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        bloodGroup.BloodGroupId = await repository.CreateAsync(bloodGroup, cancellationToken);

        return new GetBloodGroupDto
        {
            BloodGroupId = bloodGroup.BloodGroupId,
            BloodGroupName = bloodGroup.BloodGroupName,
            IsActive = bloodGroup.IsActive
        };
    }

    public Task<bool> UpdateAsync(int bloodGroupId,CreateOrUpdateBloodGroupRequest request,CancellationToken cancellationToken = default)
    {
        var bloodGroup = new BloodGroup
        {
            BloodGroupId = bloodGroupId,
            BloodGroupName = request.BloodGroupName,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        };

        return repository.UpdateAsync(bloodGroup, cancellationToken);
    }

    public Task<bool> DeleteAsync(int bloodGroupId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(bloodGroupId, cancellationToken);
}