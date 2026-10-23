namespace TeamHub.Domain.Entities;

public class CotivityDesignation : AuditableEntity<int>
{
    public int CotivityDesignationId { get; set; }

    public override int Id
    {
        get => CotivityDesignationId;
        set => CotivityDesignationId = value;
    }

    public string CotivityDesignationName { get; set; } = string.Empty;

    public int CotivityRank { get; set; }

    public bool IsActive { get; set; }
}