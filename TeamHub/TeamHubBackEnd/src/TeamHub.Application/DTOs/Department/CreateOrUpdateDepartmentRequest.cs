namespace TeamHub.Application.DTOs.Department;

public class CreateOrUpdateDepartmentRequest
{
    public string DepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}