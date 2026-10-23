using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TeamHub.Auth.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";
    public const string SuperAdminRole = "Admin";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(SuperAdminRole))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.FindAll(PermissionClaimType).Any(claim =>
                string.Equals(claim.Value, requirement.PermissionCode, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class RbacClaims
{
    public static IReadOnlyCollection<string> RoleNames(this ClaimsPrincipal user) =>
        user.FindAll("Role").Select(claim => claim.Value).ToArray();

    public static IReadOnlyCollection<string> PermissionCodes(this ClaimsPrincipal user) =>
        user.FindAll(PermissionAuthorizationHandler.PermissionClaimType).Select(claim => claim.Value).ToArray();

    public static bool IsSuperAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(PermissionAuthorizationHandler.SuperAdminRole);
}
