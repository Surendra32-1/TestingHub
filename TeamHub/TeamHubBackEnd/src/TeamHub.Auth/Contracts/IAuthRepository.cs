using TeamHub.Auth.Models;

namespace TeamHub.Auth.Contracts;

internal interface IAuthRepository
{
    Task<AuthUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    Task<AuthUser?> GetUserByEntraIdAsync(string tenantId, string objectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DevSignInUser>> GetDevSignInCandidatesAsync(
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken);
    Task<long> RecordLoginAsync(
        int userId,
        bool isSuccessful,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
    Task StoreRefreshTokenAsync(
        int userId,
        long loginHistoryId,
        string tokenHash,
        DateTimeOffset expiryDate,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
    Task<RefreshSession?> GetRefreshSessionAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> RotateRefreshTokenAsync(
        long refreshTokenId,
        int userId,
        long loginHistoryId,
        string currentTokenHash,
        string newTokenHash,
        DateTimeOffset expiryDate,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(
        int userId,
        string tokenHash,
        string? ipAddress,
        CancellationToken cancellationToken);
    Task RevokeAllRefreshTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken);
}
