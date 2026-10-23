namespace TeamHub.Application.DTOs.Employee;

public sealed class DepartmentTreeDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public IReadOnlyList<DesignationTreeDto> Designations { get; set; } = [];
}
