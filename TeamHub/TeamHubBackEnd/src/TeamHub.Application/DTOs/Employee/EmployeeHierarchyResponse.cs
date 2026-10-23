namespace TeamHub.Application.DTOs.Employee;

public sealed class EmployeeHierarchyResponse
{
    public IReadOnlyList<EmployeeHierarchyNodeDto> EmployeeManagers { get; set; } = [];
    public IReadOnlyList<CotivityHierarchyNodeDto> CotivityHierarchy { get; set; } = [];
    public SelectedEmployeeDto SelectedEmployee { get; set; } = new();
    public IReadOnlyList<EmployeeHierarchyNodeDto> ReportingHierarchy { get; set; } = [];
}


public sealed class EmployeeHierarchyNodeDto
{
    public int EmployeeId { get; set; }
    public int? ParentId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? DesignationName { get; set; }
    public string? DepartmentName { get; set; }
    public string? ImagePath { get; set; }
    public int EmployeeNumber { get; set; }
    public string Source { get; set; } = "Employee";
    public int Level { get; set; }
}

public sealed class SelectedEmployeeDto
{
    public int EmployeeId { get; set; }
    public int? ParentId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? DesignationName { get; set; }
    public string? DepartmentName { get; set; }
    public string? ImagePath { get; set; }
    public int EmployeeNumber { get; set; }
    public string Source { get; set; } = "Employee";
    public int Level { get; set; }
}

public sealed class CotivityHierarchyNodeDto
{
    public int CotivityEmployeeId { get; set; }
    public int? ParentId { get; set; }
    public string CotivityEmployeeName { get; set; } = string.Empty;
    public string? CotivityDesignationName { get; set; }
    public string? CotivityDepartmentName { get; set; }
    public string? ImagePath { get; set; }
    public string Source { get; set; } = "Cotivity";
    public int Level { get; set; }
}
