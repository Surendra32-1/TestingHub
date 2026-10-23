using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Department;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class DepartmentService(IDepartmentRepository repository) : IDepartmentService
{
    public Task<IReadOnlyList<GetDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(departmentId, cancellationToken);

    public async Task<GetDepartmentDto> CreateAsync(CreateOrUpdateDepartmentRequest request,CancellationToken cancellationToken = default)
    {
        var department = new Department
        {
            DepartmentName = request.DepartmentName,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        department.DepartmentId = await repository.CreateAsync(department, cancellationToken);

        return new GetDepartmentDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            IsActive = department.IsActive
        };
    }

    public Task<bool> UpdateAsync(int departmentId,CreateOrUpdateDepartmentRequest request,CancellationToken cancellationToken = default)
    {
        var department = new Department
        {
            DepartmentId = departmentId,
            DepartmentName = request.DepartmentName,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        };

        return repository.UpdateAsync(department, cancellationToken);
    }

    public Task<bool> DeleteAsync(int departmentId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(departmentId, cancellationToken);
}