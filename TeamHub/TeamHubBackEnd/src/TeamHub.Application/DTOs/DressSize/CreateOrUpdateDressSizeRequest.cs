namespace TeamHub.Application.DTOs.DressSize;

public sealed class CreateOrUpdateDressSizeRequest
{
    public string DressName { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}