using TeamHub.Application.DTOs.Permission;

namespace TeamHub.Application.Contracts.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<GetPermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetPermissionDto?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default);
    Task<GetPermissionDto> CreateAsync(CreateOrUpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int permissionId, CreateOrUpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int permissionId, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(int permissionId, bool isActive, CancellationToken cancellationToken = default);
}
