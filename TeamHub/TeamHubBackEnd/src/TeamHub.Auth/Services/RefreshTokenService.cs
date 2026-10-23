using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.DTOs;
using TeamHub.Auth.Infrastructure;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Services;

internal sealed class RefreshTokenService(IAuthRepository repository, IJwtService jwtService) : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<string> CreateAsync(
        int userId,
        long loginHistoryId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var token = GenerateToken();
        await repository.StoreRefreshTokenAsync(
            userId,
            loginHistoryId,
            HashToken(token),
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            ipAddress,
            userAgent,
            cancellationToken);
        return token;
    }

    public async Task<AuthenticatedSession?> RotateAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var session = await repository.GetRefreshSessionAsync(tokenHash, cancellationToken);
        if (session is null || !session.IsActive || !session.IsEmployeeActive)
        {
            return null;
        }

        if (session.RevokedDate is not null)
        {
            await repository.RevokeAllRefreshTokensAsync(session.UserId, ipAddress, cancellationToken);
            return null;
        }

        if (session.ExpiryDate <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var newToken = GenerateToken();
        var newTokenHash = HashToken(newToken);
        var didRotate = await repository.RotateRefreshTokenAsync(
            session.RefreshTokenId,
            session.UserId,
            session.LoginHistoryId,
            tokenHash,
            newTokenHash,
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            ipAddress,
            userAgent,
            cancellationToken);
        if (!didRotate)
        {
            return null;
        }

        var user = new AuthUser(
            session.UserId,
            session.EmployeeId,
            session.Username,
            session.Email,
            session.IsActive,
            session.IsEmployeeActive,
            session.DepartmentId,
            session.DesignationId,
            session.Roles,
            session.Permissions);

        return new AuthenticatedSession(jwtService.CreateAccessToken(user), newToken);
    }

    public async Task RevokeCurrentAsync(
        int userId,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await repository.RevokeRefreshTokenAsync(userId, HashToken(refreshToken), ipAddress, cancellationToken);
        }
    }

    public Task RevokeAllAsync(int userId, string? ipAddress, CancellationToken cancellationToken) =>
        repository.RevokeAllRefreshTokensAsync(userId, ipAddress, cancellationToken);

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string GenerateToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
}
