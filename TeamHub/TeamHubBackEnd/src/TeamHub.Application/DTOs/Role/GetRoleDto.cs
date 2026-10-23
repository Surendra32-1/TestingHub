namespace TeamHub.Application.DTOs.Role;

public class GetRoleDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}