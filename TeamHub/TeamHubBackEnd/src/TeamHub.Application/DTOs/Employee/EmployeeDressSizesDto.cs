namespace TeamHub.Application.DTOs.Employee;

public sealed class EmployeeDressSizesDto
{
    public int DressId  {get ;set;}
    public string DressName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
