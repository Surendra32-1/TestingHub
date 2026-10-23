using TeamHub.Application.DTOs.CotivityDepartment;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface ICotivityDepartmentRepository
{
    Task<IReadOnlyList<GetCotivityDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityDepartmentDto?> GetByIdAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CotivityDepartment department, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CotivityDepartment department, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default);
}