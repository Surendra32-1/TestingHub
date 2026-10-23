namespace TeamHub.Domain.Entities;

public class Permission : AuditableEntity<int>
{
    public int PermissionId { get; set; }

    public override int Id
    {
        get => PermissionId;
        set => PermissionId = value;
    }

    public string PermissionCode { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? Module { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
