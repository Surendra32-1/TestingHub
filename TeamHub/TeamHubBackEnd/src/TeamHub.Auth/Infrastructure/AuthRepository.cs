using Dapper;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.Models;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Auth.Infrastructure;

internal sealed class AuthRepository(IDbConnectionFactory connectionFactory) : IAuthRepository
{
    public async Task<AuthUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT u.UserId, u.EmployeeId, u.Username, u.Email,
                   u.IsActive, e.IsActive AS IsEmployeeActive, e.DepartmentId, e.DesignationId
            FROM dbo.Users u
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE u.UserId = @UserId;
            """;
        var user = await connection.QuerySingleOrDefaultAsync<AuthUserRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return user is null ? null : await MapUserAsync(connection, user, cancellationToken);
    }

    public async Task<AuthUser?> GetUserByEntraIdAsync(string tenantId, string objectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT u.UserId, u.EmployeeId, u.Username, u.Email,
                   u.IsActive, e.IsActive AS IsEmployeeActive, e.DepartmentId, e.DesignationId
            FROM dbo.Users u
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE u.EntraTenantId = @TenantId AND u.EntraObjectId = @ObjectId;
            """;
        var user = await connection.QuerySingleOrDefaultAsync<AuthUserRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, ObjectId = objectId }, cancellationToken: cancellationToken));
        return user is null ? null : await MapUserAsync(connection, user, cancellationToken);
    }

    public async Task<IReadOnlyList<DevSignInUser>> GetDevSignInCandidatesAsync(
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken)
    {
        if (allowedRoles.Count == 0)
        {
            return [];
        }

        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT u.UserId, u.EmployeeId, u.Username,
                   CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS DisplayName
            FROM dbo.Users u
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE u.IsActive = 1 AND e.IsActive = 1
            ORDER BY u.Username;
            """;
        var rows = (await connection.QueryAsync<DevSignInRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();

        var candidates = new List<DevSignInUser>(rows.Count);
        foreach (var row in rows)
        {
            var roles = await GetRolesAsync(connection, row.UserId, row.EmployeeId, cancellationToken);
            if (roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            {
                candidates.Add(new DevSignInUser(row.UserId, row.Username, row.DisplayName, roles));
            }
        }

        return candidates;
    }

    public async Task<long> RecordLoginAsync(
        int userId,
        bool isSuccessful,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string insertSql = """
            INSERT INTO dbo.UserLoginHistory (UserId, LoginDate, IpAddress, UserAgent, IsSuccessful)
            OUTPUT INSERTED.LoginHistoryId
            VALUES (@UserId, SYSUTCDATETIME(), @IpAddress, @UserAgent, @IsSuccessful);
            """;
        var historyId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                insertSql,
                new { UserId = userId, IpAddress = Limit(ipAddress, 50), UserAgent = Limit(userAgent, 500), IsSuccessful = isSuccessful },
                transaction,
                cancellationToken: cancellationToken));

        if (isSuccessful)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Users SET LastLoginDate = SYSUTCDATETIME() WHERE UserId = @UserId;",
                new { UserId = userId },
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
        return historyId;
    }

    public async Task StoreRefreshTokenAsync(
        int userId,
        long loginHistoryId,
        string tokenHash,
        DateTimeOffset expiryDate,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO dbo.RefreshTokens
                (UserId, LoginHistoryId, Token, ExpiryDate, CreatedDate, CreatedByIp, UserAgent)
            VALUES
                (@UserId, @LoginHistoryId, @TokenHash, @ExpiryDate, SYSUTCDATETIME(), @IpAddress, @UserAgent);
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                UserId = userId,
                LoginHistoryId = loginHistoryId,
                TokenHash = tokenHash,
                ExpiryDate = expiryDate.UtcDateTime,
                IpAddress = Limit(ipAddress, 50),
                UserAgent = Limit(userAgent, 500)
            },
            cancellationToken: cancellationToken));
    }

    public async Task<RefreshSession?> GetRefreshSessionAsync(string tokenHash, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT rt.RefreshTokenId, rt.UserId, rt.Token AS TokenHash, rt.ExpiryDate,
                   rt.RevokedDate, rt.LoginHistoryId, u.Username, u.Email, u.IsActive,
                   e.IsActive AS IsEmployeeActive, u.EmployeeId, e.DepartmentId, e.DesignationId
            FROM dbo.RefreshTokens rt
            INNER JOIN dbo.Users u ON u.UserId = rt.UserId
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE rt.Token = @TokenHash;
            """;
        var session = await connection.QuerySingleOrDefaultAsync<RefreshSessionRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
        if (session is null)
        {
            return null;
        }

        var roles = await GetRolesAsync(connection, session.UserId, session.EmployeeId, cancellationToken);
        var permissions = await GetPermissionsAsync(connection, session.UserId, cancellationToken);
        return new RefreshSession(
            session.RefreshTokenId,
            session.UserId,
            session.TokenHash,
            session.ExpiryDate,
            session.RevokedDate,
            session.LoginHistoryId,
            session.Username,
            session.Email,
            session.IsActive,
            session.IsEmployeeActive,
            session.EmployeeId,
            session.DepartmentId,
            session.DesignationId,
            roles,
            permissions);
    }

    public async Task<bool> RotateRefreshTokenAsync(
        long refreshTokenId,
        int userId,
        long loginHistoryId,
        string currentTokenHash,
        string newTokenHash,
        DateTimeOffset expiryDate,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        const string revokeSql = """
            UPDATE dbo.RefreshTokens
            SET RevokedDate = SYSUTCDATETIME(), RevokedByIp = @IpAddress, ReplacedByToken = @NewTokenHash
            WHERE RefreshTokenId = @RefreshTokenId AND Token = @CurrentTokenHash
              AND RevokedDate IS NULL AND ExpiryDate > SYSUTCDATETIME();
            """;
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            revokeSql,
            new
            {
                RefreshTokenId = refreshTokenId,
                CurrentTokenHash = currentTokenHash,
                NewTokenHash = newTokenHash,
                IpAddress = Limit(ipAddress, 50)
            },
            transaction,
            cancellationToken: cancellationToken));

        if (updated == 0)
        {
            const string revokeAllSql = """
                UPDATE dbo.RefreshTokens
                SET RevokedDate = SYSUTCDATETIME(), RevokedByIp = @IpAddress
                WHERE UserId = @UserId AND RevokedDate IS NULL;
                UPDATE dbo.UserLoginHistory
                SET LogoutDate = SYSUTCDATETIME()
                WHERE UserId = @UserId AND IsSuccessful = 1 AND LogoutDate IS NULL;
                """;
            await connection.ExecuteAsync(new CommandDefinition(
                revokeAllSql,
                new { UserId = userId, IpAddress = Limit(ipAddress, 50) },
                transaction,
                cancellationToken: cancellationToken));
            transaction.Commit();
            return false;
        }

        const string insertSql = """
            INSERT INTO dbo.RefreshTokens
                (UserId, LoginHistoryId, Token, ExpiryDate, CreatedDate, CreatedByIp, UserAgent)
            VALUES
                (@UserId, @LoginHistoryId, @TokenHash, @ExpiryDate, SYSUTCDATETIME(), @IpAddress, @UserAgent);
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            new
            {
                UserId = userId,
                LoginHistoryId = loginHistoryId,
                TokenHash = newTokenHash,
                ExpiryDate = expiryDate.UtcDateTime,
                IpAddress = Limit(ipAddress, 50),
                UserAgent = Limit(userAgent, 500)
            },
            transaction,
            cancellationToken: cancellationToken));
        transaction.Commit();
        return true;
    }

    public async Task RevokeRefreshTokenAsync(
        int userId,
        string tokenHash,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE rt
            SET rt.RevokedDate = SYSUTCDATETIME(), rt.RevokedByIp = @IpAddress
            FROM dbo.RefreshTokens rt
            WHERE rt.UserId = @UserId AND rt.Token = @TokenHash AND rt.RevokedDate IS NULL;
            UPDATE h
            SET h.LogoutDate = SYSUTCDATETIME()
            FROM dbo.UserLoginHistory h
            INNER JOIN dbo.RefreshTokens rt ON rt.LoginHistoryId = h.LoginHistoryId
            WHERE rt.UserId = @UserId AND rt.Token = @TokenHash AND h.LogoutDate IS NULL;
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, TokenHash = tokenHash, IpAddress = Limit(ipAddress, 50) },
            cancellationToken: cancellationToken));
    }

    public async Task RevokeAllRefreshTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE dbo.RefreshTokens
            SET RevokedDate = SYSUTCDATETIME(), RevokedByIp = @IpAddress
            WHERE UserId = @UserId AND RevokedDate IS NULL;
            UPDATE dbo.UserLoginHistory
            SET LogoutDate = SYSUTCDATETIME()
            WHERE UserId = @UserId AND IsSuccessful = 1 AND LogoutDate IS NULL;
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, IpAddress = Limit(ipAddress, 50) },
            cancellationToken: cancellationToken));
    }

    private static async Task<AuthUser> MapUserAsync(
        System.Data.IDbConnection connection,
        AuthUserRow user,
        CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(connection, user.UserId, user.EmployeeId, cancellationToken);
        var permissions = await GetPermissionsAsync(connection, user.UserId, cancellationToken);
        return new AuthUser(
            user.UserId,
            user.EmployeeId,
            user.Username,
            user.Email,
            user.IsActive,
            user.IsEmployeeActive,
            user.DepartmentId,
            user.DesignationId,
            roles,
            permissions);
    }

    private static async Task<IReadOnlyList<string>> GetRolesAsync(
        System.Data.IDbConnection connection,
        int userId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        const string junctionSql = """
            SELECT r.RoleName
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId AND r.IsActive = 1
            WHERE ur.UserId = @UserId
            ORDER BY r.RoleName;
            """;
        var roles = (await connection.QueryAsync<string>(
            new CommandDefinition(junctionSql, new { UserId = userId }, cancellationToken: cancellationToken))).AsList();
        return roles;
    }

    private static async Task<IReadOnlyList<string>> GetPermissionsAsync(
        System.Data.IDbConnection connection,
        int userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT p.PermissionCode
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId AND r.IsActive = 1
            INNER JOIN dbo.RolePermissions rp ON rp.RoleId = ur.RoleId
            INNER JOIN dbo.Permissions p ON p.PermissionId = rp.PermissionId AND p.IsActive = 1
            WHERE ur.UserId = @UserId
            ORDER BY p.PermissionCode;
            """;
        var permissions = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return permissions.AsList();
    }

    private static string? Limit(string? value, int maxLength) =>
        value is { Length: > 0 } ? value[..Math.Min(value.Length, maxLength)] : null;

    private sealed record AuthUserRow(
        int UserId,
        int EmployeeId,
        string Username,
        string Email,
        bool IsActive,
        bool IsEmployeeActive,
        int DepartmentId,
        int DesignationId);

    private sealed record DevSignInRow(
        int UserId,
        int EmployeeId,
        string Username,
        string DisplayName);

    private sealed record RefreshSessionRow(
        long RefreshTokenId,
        int UserId,
        string TokenHash,
        DateTimeOffset ExpiryDate,
        DateTimeOffset? RevokedDate,
        long LoginHistoryId,
        string Username,
        string Email,
        bool IsActive,
        bool IsEmployeeActive,
        int EmployeeId,
        int DepartmentId,
        int DesignationId);
}

internal sealed class AuthValidationException(string message) : Exception(message)
{
}
