using Microsoft.AspNetCore.Authorization;

namespace TeamHub.Auth.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
