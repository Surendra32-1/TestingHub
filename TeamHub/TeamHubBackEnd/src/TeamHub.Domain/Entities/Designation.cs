namespace TeamHub.Domain.Entities;

public class Designation : AuditableEntity<int>
{
    public int DesignationId { get; set; }

    public override int Id
    {
        get => DesignationId;
        set => DesignationId = value;
    }

    public string DesignationName { get; set; } = string.Empty;

    public int Rank { get; set; }

    public bool IsActive { get; set; }
}