namespace TeamHub.Application.DTOs.Employee;

public sealed class EmployeeSearchFilter
{
    public string? Name { get; init; }

    public string? Designation { get; init; }

    public int? DepartmentId { get; init; }

    public string? Contact { get; init; }
}
