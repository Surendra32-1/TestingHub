using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using TeamHub.Auth.Contracts;

namespace TeamHub.Auth.Entra;

internal static class EntraClaimsIssuer
{
    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var tenantId = principal?.FindFirst("tid")?.Value;
        var objectId = principal?.FindFirst("oid")?.Value;

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(objectId))
        {
            context.Fail("The Entra token is missing the required tid/oid claims.");
            return;
        }

        var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthRepository>();
        var user = await repository.GetUserByEntraIdAsync(tenantId, objectId, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || !user.IsEmployeeActive)
        {
            context.Fail("No matching active local account exists for the authenticated Entra identity.");
            return;
        }

        var identity = (ClaimsIdentity)principal!.Identity!;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        identity.AddClaim(new Claim("UserId", user.UserId.ToString(invariant)));
        identity.AddClaim(new Claim("EmployeeId", user.EmployeeId.ToString(invariant)));
        identity.AddClaim(new Claim("Username", user.Username));
        identity.AddClaim(new Claim("Email", user.Email));
        identity.AddClaim(new Claim("DepartmentId", user.DepartmentId.ToString(invariant)));
        identity.AddClaim(new Claim("DesignationId", user.DesignationId.ToString(invariant)));

        foreach (var role in user.Roles)
        {
            identity.AddClaim(new Claim("Role", role));
        }

        foreach (var permission in user.Permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }
    }
}
