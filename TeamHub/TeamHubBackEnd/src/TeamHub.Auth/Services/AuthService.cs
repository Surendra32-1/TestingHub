using Microsoft.Extensions.Configuration;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.DTOs;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Services;

internal sealed class AuthService(
    IAuthRepository repository,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IConfiguration configuration) : IAuthService
{
    private static readonly string[] DefaultAllowedDevRoles = ["Admin", "NormalUser"];

    public async Task<AuthenticatedSession?> DevSignInAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || !user.IsEmployeeActive)
        {
            return null;
        }

        var allowedRoles = GetAllowedDevRoles();
        if (!user.Roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            return null;
        }

        var loginHistoryId = await repository.RecordLoginAsync(
            user.UserId,
            true,
            ipAddress,
            userAgent,
            cancellationToken);
        var refreshToken = await refreshTokenService.CreateAsync(
            user.UserId,
            loginHistoryId,
            ipAddress,
            userAgent,
            cancellationToken);
        return new AuthenticatedSession(jwtService.CreateAccessToken(user), refreshToken);
    }

    public async Task<IReadOnlyList<DevSignInUser>> GetDevSignInUsersAsync(CancellationToken cancellationToken)
    {
        var allowedRoles = GetAllowedDevRoles();
        return await repository.GetDevSignInCandidatesAsync(allowedRoles, cancellationToken);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || !user.IsEmployeeActive)
        {
            return null;
        }

        return new CurrentUserResponse(
            user.UserId,
            user.EmployeeId,
            user.Username,
            user.Email,
            user.DepartmentId,
            user.DesignationId,
            user.Roles,
            user.Permissions);
    }

    private IReadOnlyCollection<string> GetAllowedDevRoles() =>
        configuration.GetSection("Auth:DevSignIn:AllowedRoles").Get<string[]>() is { Length: > 0 } configured
            ? configured
            : DefaultAllowedDevRoles;
}
