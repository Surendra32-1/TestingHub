namespace TeamHub.Application.DTOs.Employee;

public sealed class DesignationTreeDto
{
    public string Designation { get; set; } = string.Empty;
    public IReadOnlyList<EmployeeNodeDto> Employees { get; set; } = [];
}
