namespace TeamHub.Application.DTOs.Skill;

public class GetSkillDto
{
    public int SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}