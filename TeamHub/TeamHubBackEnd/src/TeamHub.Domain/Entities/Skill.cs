namespace TeamHub.Domain.Entities;

public class Skill : AuditableEntity<int>
{
    public int SkillId { get; set; }

    public override int Id
    {
        get => SkillId;
        set => SkillId = value;
    }

    public string SkillName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}