using TeamHub.Auth.DTOs;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Contracts;

public interface IAuthService
{
    Task<AuthenticatedSession?> DevSignInAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DevSignInUser>> GetDevSignInUsersAsync(CancellationToken cancellationToken);

    Task<CurrentUserResponse?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken);
}

public sealed record AuthenticatedSession(AuthResponse Response, string RefreshToken);
