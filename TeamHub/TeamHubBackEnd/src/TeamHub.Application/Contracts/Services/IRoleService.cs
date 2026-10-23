using TeamHub.Application.DTOs.Role;

namespace TeamHub.Application.Contracts.Services;

public interface IRoleService
{
    Task<IReadOnlyList<GetRoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetRoleDto?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<GetRoleDto> CreateAsync(CreateOrUpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int roleId, CreateOrUpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int roleId, CancellationToken cancellationToken = default);
}