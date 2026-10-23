namespace TeamHub.Domain.Entities;

public class EmployeeSkill : AuditableEntity<int>
{
    public int EmployeeSkillId { get; set; }

    public override int Id
    {
        get => EmployeeSkillId;
        set => EmployeeSkillId = value;
    }

    public int EmployeeId { get; set; }
    public int SkillId { get; set; }
    public int SkillLevel { get; set; }
    public bool IsPrimary { get; set; }
    public Employee? Employee { get; set; }
    public Skill? Skill { get; set; }
}