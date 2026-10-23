namespace TeamHub.Domain.Entities;

public class Team : AuditableEntity<int>
{
    public int TeamId { get; set; }

    public override int Id
    {
        get => TeamId;
        set => TeamId = value;
    }

    public string TeamName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}