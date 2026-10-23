using TeamHub.Application.DTOs.Permission;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<GetPermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetPermissionDto?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string permissionCode, int? excludePermissionId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int permissionId, CancellationToken cancellationToken = default);
}
