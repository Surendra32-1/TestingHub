namespace TeamHub.Domain.Entities;

public class Role : AuditableEntity<int>
{
    public int RoleId { get; set; }

    public override int Id
    {
        get => RoleId;
        set => RoleId = value;
    }

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}