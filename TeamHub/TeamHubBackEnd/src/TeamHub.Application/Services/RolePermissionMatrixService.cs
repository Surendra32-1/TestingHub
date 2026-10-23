using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Services;

public sealed class RolePermissionMatrixService(
    IRolePermissionRepository repository,
    IPermissionRepository permissionRepository) : IRolePermissionMatrixService
{
    public Task<RolePermissionMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default) =>
        repository.GetMatrixAsync(pageNumber, pageSize, search, cancellationToken);

    public async Task SaveAsync(SaveRolePermissionsRequest request, IReadOnlyCollection<string> actingPermissionCodes, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        if (request.Assignments is null || request.Assignments.Count == 0)
        {
            return;
        }

        if (!isSuperAdmin)
        {
            var codesById = (await permissionRepository.GetAllAsync(cancellationToken))
                .ToDictionary(permission => permission.PermissionId, permission => permission.PermissionCode);
            var held = new HashSet<string>(actingPermissionCodes, StringComparer.OrdinalIgnoreCase);

            foreach (var assignment in request.Assignments)
            {
                if (assignment.RoleIds.Count == 0)
                {
                    continue;
                }

                if (!codesById.TryGetValue(assignment.PermissionId, out var code))
                {
                    throw new RbacValidationException($"Permission id {assignment.PermissionId} does not exist or is inactive.");
                }

                if (!held.Contains(code))
                {
                    throw new PrivilegeEscalationException(
                        $"You cannot grant the permission '{code}' because you do not currently hold it.");
                }
            }
        }

        await repository.SaveAsync(request.Assignments, "System", cancellationToken);
    }
}
