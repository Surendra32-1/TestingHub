namespace TeamHub.Application.DTOs.CotivityEmployee;

public class GetCotivityEmployeeDto
{
    public int CotivityEmployeeId { get; set; }

    public string CotivityEmployeeName { get; set; } = string.Empty;

    public int CotivityDepartmentId { get; set; }

    public string CotivityDepartmentName { get; set; } = string.Empty;

    public int CotivityDepartmentDesignationId { get; set; }

    public string CotivityDepartmentDesignationName { get; set; } = string.Empty;

    public int? ReportsToCotivityEmployeeId { get; set; }

    public string? ReportsToCotivityEmployeeName { get; set; }

    public bool IsActive { get; set; }

    public string? ImageBase64 { get; set; }
}