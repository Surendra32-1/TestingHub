using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Services;

public sealed class UserRoleMatrixService(
    IUserRoleRepository repository,
    IRoleRepository roleRepository) : IUserRoleMatrixService
{
    public Task<EmployeeRoleMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default) =>
        repository.GetMatrixAsync(pageNumber, pageSize, search, cancellationToken);

    public async Task SaveAsync(SaveUserRolesRequest request, IReadOnlyCollection<string> actingRoleNames, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        if (request.Assignments is null || request.Assignments.Count == 0)
        {
            return;
        }

        if (!isSuperAdmin)
        {
            var rolesById = (await roleRepository.GetAllAsync(cancellationToken))
                .ToDictionary(role => role.RoleId, role => role.RoleName);
            var held = new HashSet<string>(actingRoleNames, StringComparer.OrdinalIgnoreCase);

            var allRoleIds = request.Assignments.SelectMany(assignment => assignment.RoleIds).Distinct();
            foreach (var roleId in allRoleIds)
            {
                if (!rolesById.TryGetValue(roleId, out var roleName))
                {
                    throw new RbacValidationException($"Role id {roleId} does not exist or is inactive.");
                }

                if (!held.Contains(roleName))
                {
                    throw new PrivilegeEscalationException(
                        $"You cannot grant the role '{roleName}' because you do not currently hold it.");
                }
            }
        }

        await repository.SaveAsync(request.Assignments, actingRoleNames.FirstOrDefault() ?? "System", cancellationToken);
    }
}
