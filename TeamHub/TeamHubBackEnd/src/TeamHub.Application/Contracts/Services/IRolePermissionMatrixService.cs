using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Contracts.Services;

public interface IRolePermissionMatrixService
{
    Task<RolePermissionMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);

    Task SaveAsync(SaveRolePermissionsRequest request, IReadOnlyCollection<string> actingPermissionCodes, bool isSuperAdmin, CancellationToken cancellationToken = default);
}
