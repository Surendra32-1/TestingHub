using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.BusinessUnit;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class BusinessUnitService(IBusinessUnitRepository repository) : IBusinessUnitService
{
    public Task<IReadOnlyList<GetBusinessUnitDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetBusinessUnitDto?> GetByIdAsync(int businessUnitId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(businessUnitId, cancellationToken);

    public async Task<GetBusinessUnitDto> CreateAsync(CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new BusinessUnit
        {
            BusinessUnitName = request.BusinessUnitName.Trim(),
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };
        entity.BusinessUnitId = await repository.CreateAsync(entity, cancellationToken);
        return new GetBusinessUnitDto
        {
            BusinessUnitId = entity.BusinessUnitId,
            BusinessUnitName = entity.BusinessUnitName,
            IsActive = entity.IsActive
        };
    }

    public Task<bool> UpdateAsync(int businessUnitId, CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new BusinessUnit
        {
            BusinessUnitId = businessUnitId,
            BusinessUnitName = request.BusinessUnitName.Trim(),
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        }, cancellationToken);

    public Task<bool> DeleteAsync(int businessUnitId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(businessUnitId, cancellationToken);
}