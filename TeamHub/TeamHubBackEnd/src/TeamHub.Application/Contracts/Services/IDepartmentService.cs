using TeamHub.Application.DTOs.Department;

namespace TeamHub.Application.Contracts.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<GetDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default);

    Task<GetDepartmentDto> CreateAsync(CreateOrUpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int departmentId, CreateOrUpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int departmentId, CancellationToken cancellationToken = default);
}