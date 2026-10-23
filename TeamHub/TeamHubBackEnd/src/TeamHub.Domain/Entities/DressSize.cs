namespace TeamHub.Domain.Entities;

public sealed class DressSize : AuditableEntity<int>
{
    public int DressId { get; set; }

    public override int Id
    {
        get => DressId;
        set => DressId = value;
    }

    public string DressName { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}