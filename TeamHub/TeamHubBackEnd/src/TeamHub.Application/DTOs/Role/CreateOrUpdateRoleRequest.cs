namespace TeamHub.Application.DTOs.Role;

public class CreateOrUpdateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}