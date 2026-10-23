using TeamHub.Application.DTOs.Role;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<GetRoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetRoleDto?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int roleId, CancellationToken cancellationToken = default);
}