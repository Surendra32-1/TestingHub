namespace TeamHub.Application.DTOs.BusinessUnit;

public sealed class CreateOrUpdateBusinessUnitRequest
{
    public string BusinessUnitName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}