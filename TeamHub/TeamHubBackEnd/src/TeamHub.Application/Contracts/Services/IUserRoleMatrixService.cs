using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Contracts.Services;

public interface IUserRoleMatrixService
{
    Task<EmployeeRoleMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);

    Task SaveAsync(SaveUserRolesRequest request, IReadOnlyCollection<string> actingRoleNames, bool isSuperAdmin, CancellationToken cancellationToken = default);
}
