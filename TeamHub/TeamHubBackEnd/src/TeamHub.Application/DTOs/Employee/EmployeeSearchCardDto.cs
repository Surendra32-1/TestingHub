namespace TeamHub.Application.DTOs.Employee;

public class EmployeeSearchCardDto
{
    public int Id { get; set; }
    public int ENumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? TeamName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int ExtensionNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? StationNumber { get; set; }
    public string? ReportsToEmployee { get; set; }
    public string? ImageBase64 { get; set; }
}
