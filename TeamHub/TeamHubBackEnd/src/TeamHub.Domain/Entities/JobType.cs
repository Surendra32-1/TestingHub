namespace TeamHub.Domain.Entities;

public class JobType : AuditableEntity<int>
{
    public int JobTypeId { get; set; }

    public override int Id
    {
        get => JobTypeId;
        set => JobTypeId = value;
    }

    public string JobTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}