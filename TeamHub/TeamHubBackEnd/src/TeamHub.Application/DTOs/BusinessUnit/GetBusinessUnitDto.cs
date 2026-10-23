namespace TeamHub.Application.DTOs.BusinessUnit;

public sealed class GetBusinessUnitDto
{
    public int BusinessUnitId { get; set; }

    public string BusinessUnitName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}