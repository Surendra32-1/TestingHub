namespace TeamHub.Application.DTOs.Employee;

public sealed class EmployeeNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class EmployeeReportingManagerDto
{
    public int ReportingManagerId { get; set; }
    public string ReportingManagerName { get; set; } = string.Empty;
}
