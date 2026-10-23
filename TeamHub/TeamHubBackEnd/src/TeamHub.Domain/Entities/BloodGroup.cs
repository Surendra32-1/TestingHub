namespace TeamHub.Domain.Entities;

public class BloodGroup : AuditableEntity<int>
{
    public int BloodGroupId { get; set; }

    public override int Id
    {
        get => BloodGroupId;
        set => BloodGroupId = value;
    }

    public string BloodGroupName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}