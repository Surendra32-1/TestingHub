namespace TeamHub.Application.DTOs.Permission;

public class GetPermissionDto
{
    public int PermissionId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? Module { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
