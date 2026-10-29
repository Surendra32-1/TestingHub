namespace TeamHub.Application.DTOs.Employee;

public class GetActiveInactiveEmployeeDto
{
    public int Id { get; set; }
    public int ENumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ReportsToEmployee { get; set; }
    public string? TeamName { get; set; }
    public string? BusinessUnitName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
