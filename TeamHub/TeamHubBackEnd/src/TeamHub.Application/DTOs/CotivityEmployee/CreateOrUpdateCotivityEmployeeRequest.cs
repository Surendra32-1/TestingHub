namespace TeamHub.Application.DTOs.CotivityEmployee;

public class CreateOrUpdateCotivityEmployeeRequest
{
    public string CotivityEmployeeName { get; set; } = string.Empty;

    public int CotivityDepartmentId { get; set; }

    public int CotivityDepartmentDesignationId { get; set; }

    public int? ReportsToCotivityEmployeeId { get; set; }

    public string? ImageBase64 { get; set; }

    public bool IsActive { get; set; }
}