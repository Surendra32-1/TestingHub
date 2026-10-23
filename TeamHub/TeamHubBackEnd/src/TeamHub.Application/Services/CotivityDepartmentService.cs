using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityDepartment;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class CotivityDepartmentService(ICotivityDepartmentRepository repository) : ICotivityDepartmentService
{
    public Task<IReadOnlyList<GetCotivityDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetCotivityDepartmentDto?> GetByIdAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(cotivityDepartmentId, cancellationToken);

    public async Task<GetCotivityDepartmentDto> CreateAsync(CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = new CotivityDepartment
        {
            CotivityDepartmentName = request.CotivityDepartmentName,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        department.CotivityDepartmentId = await repository.CreateAsync(department, cancellationToken);

        return new GetCotivityDepartmentDto
        {
            CotivityDepartmentId = department.CotivityDepartmentId,
            CotivityDepartmentName = department.CotivityDepartmentName,
            IsActive = department.IsActive
        };
    }

    public Task<bool> UpdateAsync(int cotivityDepartmentId, CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new CotivityDepartment
        {
            CotivityDepartmentId = cotivityDepartmentId,
            CotivityDepartmentName = request.CotivityDepartmentName,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        }, cancellationToken);

    public Task<bool> DeleteAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(cotivityDepartmentId, cancellationToken);
}