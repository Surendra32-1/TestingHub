namespace TeamHub.Domain.Entities;

public class UserRole : AuditableEntity<int>
{
    public int UserRoleId { get; set; }

    public override int Id
    {
        get => UserRoleId;
        set => UserRoleId = value;
    }

    public int UserId { get; set; }

    public int RoleId { get; set; }
}
