namespace TeamHub.Domain.Entities;

public class CotivityDepartment : AuditableEntity<int>
{
    public int CotivityDepartmentId { get; set; }

    public override int Id
    {
        get => CotivityDepartmentId;
        set => CotivityDepartmentId = value;
    }

    public string CotivityDepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}