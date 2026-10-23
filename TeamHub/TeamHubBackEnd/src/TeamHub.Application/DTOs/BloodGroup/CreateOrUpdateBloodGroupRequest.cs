namespace TeamHub.Application.DTOs.BloodGroup;

public class CreateOrUpdateBloodGroupRequest
{
    public string BloodGroupName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}