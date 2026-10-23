namespace TeamHub.Application.DTOs.Gender;

public class GetGenderDto
{
    public int GenderId { get; set; }

    public string GenderName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}