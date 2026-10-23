namespace TeamHub.Domain.Entities;

public class Recruitment : AuditableEntity<int>
{
    public int RecruitmentId { get; set; }

    public override int Id
    {
        get => RecruitmentId;
        set => RecruitmentId = value;
    }

    public string RecruitmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}