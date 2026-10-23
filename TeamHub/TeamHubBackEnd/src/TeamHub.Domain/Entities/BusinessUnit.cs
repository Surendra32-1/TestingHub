namespace TeamHub.Domain.Entities;

public sealed class BusinessUnit : AuditableEntity<int>
{
    public int BusinessUnitId { get; set; }

    public override int Id
    {
        get => BusinessUnitId;
        set => BusinessUnitId = value;
    }

    public string BusinessUnitName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}