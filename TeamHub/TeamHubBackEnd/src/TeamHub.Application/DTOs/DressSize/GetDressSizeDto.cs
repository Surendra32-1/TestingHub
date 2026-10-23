namespace TeamHub.Application.DTOs.DressSize;

public sealed class GetDressSizeDto
{
    public int DressId { get; set; }

    public string DressName { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}