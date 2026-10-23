namespace TeamHub.Auth.Models;

public sealed record AuthUser(
    int UserId,
    int EmployeeId,
    string Username,
    string Email,
    bool IsActive,
    bool IsEmployeeActive,
    int DepartmentId,
    int DesignationId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record DevSignInUser(
    int UserId,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles);

internal sealed record RefreshSession(
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
    int DesignationId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
