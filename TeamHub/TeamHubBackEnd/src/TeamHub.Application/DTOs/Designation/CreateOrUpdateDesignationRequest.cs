namespace TeamHub.Application.DTOs.Designation;

public class CreateOrUpdateDesignationRequest
{
    public string DesignationName { get; set; } = string.Empty;

    public int Rank { get; set; }

    public bool IsActive { get; set; }
}