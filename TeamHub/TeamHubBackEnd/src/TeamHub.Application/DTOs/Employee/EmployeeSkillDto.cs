namespace TeamHub.Application.DTOs.Employee;

public class EmployeeSkillDto
{
    public int EmployeeSkillId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int SkillLevel { get; set; }
    public bool IsPrimary { get; set; }
}