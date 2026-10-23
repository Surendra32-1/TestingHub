using System.ComponentModel.DataAnnotations;

namespace TeamHub.Auth.DTOs;

public sealed record DevSignInRequest
{
    [Range(1, int.MaxValue)]
    public required int UserId { get; init; }
}

public sealed record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record CurrentUserResponse(
    int UserId,
    int EmployeeId,
    string Username,
    string Email,
    int DepartmentId,
    int DesignationId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
