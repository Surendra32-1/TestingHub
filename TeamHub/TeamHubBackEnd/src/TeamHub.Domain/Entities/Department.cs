namespace TeamHub.Domain.Entities;

public class Department : AuditableEntity<int>
{
    public int DepartmentId { get; set; }

    public override int Id
    {
        get => DepartmentId;
        set => DepartmentId = value;
    }

    public string DepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

}