using TeamHub.Application.DTOs.Rbac;

namespace TeamHub.Application.Contracts.Repositories;

public interface IUserRoleRepository
{
    Task<EmployeeRoleMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<UserRoleAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default);
}
