namespace TeamHub.Application.DTOs.Employee;

public sealed class BusinessUnitDto
{
    public int BusinessUnitId { get; set; }

    public string BusinessUnitName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

}
