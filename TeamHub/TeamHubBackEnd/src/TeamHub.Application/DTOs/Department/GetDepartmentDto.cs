namespace TeamHub.Application.DTOs.Department;

public class GetDepartmentDto
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}