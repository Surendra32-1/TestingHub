namespace TeamHub.Domain.Entities;

public class CotivityEmployee : AuditableEntity<int>
{
    public int CotivityEmployeeId { get; set; }

    public override int Id
    {
        get => CotivityEmployeeId;
        set => CotivityEmployeeId = value;
    }

    public string CotivityEmployeeName { get; set; } = string.Empty;

    public int CotivityDepartmentId { get; set; }

    public int CotivityDepartmentDesignationId { get; set; }

    public int? ReportsToCotivityEmployeeId { get; set; }

    public bool IsActive { get; set; }
}