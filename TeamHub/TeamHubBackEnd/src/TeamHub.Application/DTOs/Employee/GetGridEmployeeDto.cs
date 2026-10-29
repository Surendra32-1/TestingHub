namespace TeamHub.Application.DTOs.Employee;

public class GetGridEmployeeDto
{
    public int Id { get; set; }
    public int ENumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int DesignationRank { get; set; }
    public string? Department { get; set; }
    public DateOnly JoinedDate { get; set; }
    public string? PrimarySkill { get; set; }
    public bool IsActive { get; set; }
    public string? Gender { get; set; }
    public int? ReportsToEmployeeId { get; set; }
    public string? ReportsToEmployee { get; set; }
    public string? LeadEmployee { get; set; }
    public string? StationNumber { get; set; }
    public int? RecruitmentId { get; set; }
    public int ExtensionNumber { get; set; }
    public string CitizenshipNumber { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string PANNumber { get; set; } = string.Empty;
    public string? LockerNumber { get; set; }
    public string? RoleName { get; set; }
    public string? TeamName { get; set; }
    public string? ReportToCotivityEmployee { get; set; }
    public string? ImageBase64 { get; set; }
    public IReadOnlyList<string> Addresses { get; set; } = [];
    public IReadOnlyList<string> Skills { get; set; } = [];
}