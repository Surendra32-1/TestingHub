namespace TeamHub.Domain.Entities;

public class RolePermission : AuditableEntity<int>
{
    public int RolePermissionId { get; set; }

    public override int Id
    {
        get => RolePermissionId;
        set => RolePermissionId = value;
    }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }
}
