namespace TeamHub.Application.DTOs.BloodGroup;

public class GetBloodGroupDto
{
    public int BloodGroupId { get; set; }

    public string BloodGroupName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}