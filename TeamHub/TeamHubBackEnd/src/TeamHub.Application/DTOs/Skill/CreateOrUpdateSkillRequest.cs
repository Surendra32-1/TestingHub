namespace TeamHub.Application.DTOs.Skill;

public class CreateOrUpdateSkillRequest
{
    public string SkillName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}