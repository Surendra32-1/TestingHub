using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Permission;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class PermissionService(IPermissionRepository repository) : IPermissionService
{
    public Task<IReadOnlyList<GetPermissionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetPermissionDto?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(permissionId, cancellationToken);

    public async Task<GetPermissionDto> CreateAsync(CreateOrUpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.PermissionCode.Trim();
        if (await repository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new RbacValidationException($"Permission code '{code}' already exists.");
        }

        var permission = new Permission
        {
            PermissionCode = code,
            PermissionName = request.PermissionName.Trim(),
            Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };
        permission.PermissionId = await repository.CreateAsync(permission, cancellationToken);
        return ToDto(permission);
    }

    public async Task<bool> UpdateAsync(int permissionId, CreateOrUpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.PermissionCode.Trim();
        if (await repository.CodeExistsAsync(code, permissionId, cancellationToken))
        {
            throw new RbacValidationException($"Permission code '{code}' already exists.");
        }

        var permission = new Permission
        {
            PermissionId = permissionId,
            PermissionCode = code,
            PermissionName = request.PermissionName.Trim(),
            Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        };
        return await repository.UpdateAsync(permission, cancellationToken);
    }

    public Task<bool> DeleteAsync(int permissionId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(permissionId, cancellationToken);

    public async Task<bool> SetActiveAsync(int permissionId, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(permissionId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var permission = new Permission
        {
            PermissionId = existing.PermissionId,
            PermissionCode = existing.PermissionCode,
            PermissionName = existing.PermissionName,
            Module = existing.Module,
            Description = existing.Description,
            IsActive = isActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        };
        return await repository.UpdateAsync(permission, cancellationToken);
    }

    private static GetPermissionDto ToDto(Permission permission) => new()
    {
        PermissionId = permission.PermissionId,
        PermissionCode = permission.PermissionCode,
        PermissionName = permission.PermissionName,
        Module = permission.Module,
        Description = permission.Description,
        IsActive = permission.IsActive
    };
}
