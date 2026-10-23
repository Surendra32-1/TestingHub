namespace TeamHub.Domain.Entities;

public class EmployeeDress : AuditableEntity<int>
{
    public int EmployeeDressId { get; set; }

    public override int Id
    {
        get => EmployeeDressId;
        set => EmployeeDressId = value;
    }

    public int EmployeeId { get; set; }
    public int DressId { get; set; }

    public Employee? Employee { get; set; }
    public DressSize? DressSize { get; set; }
}