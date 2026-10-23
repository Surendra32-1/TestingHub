using TeamHub.Application.DTOs.Department;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<GetDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Department department, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Department department, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int departmentId, CancellationToken cancellationToken = default);
}