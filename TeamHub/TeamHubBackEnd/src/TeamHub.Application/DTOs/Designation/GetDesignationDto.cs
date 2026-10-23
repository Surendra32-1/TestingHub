namespace TeamHub.Application.DTOs.Designation;

public class GetDesignationDto
{
    public int DesignationId { get; set; }

    public string DesignationName { get; set; } = string.Empty;

    public int Rank { get; set; }

    public bool IsActive { get; set; }
}