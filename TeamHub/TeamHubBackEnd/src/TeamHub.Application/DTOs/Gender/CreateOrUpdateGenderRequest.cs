namespace TeamHub.Application.DTOs.Gender;

public class CreateOrUpdateGenderRequest
{
    public string GenderName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}