using TeamHub.Auth.Models;

namespace TeamHub.Auth.Contracts;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(
        int userId,
        long loginHistoryId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthenticatedSession?> RotateAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task RevokeCurrentAsync(
        int userId,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task RevokeAllAsync(
        int userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
