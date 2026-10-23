using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Contracts.Repositories;

public interface IRolePermissionRepository
{
    Task<RolePermissionMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<RolePermissionAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default);
}
