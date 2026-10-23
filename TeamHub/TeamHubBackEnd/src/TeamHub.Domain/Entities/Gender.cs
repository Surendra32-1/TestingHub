namespace TeamHub.Domain.Entities;

public class Gender : AuditableEntity<int>
{
    public int GenderId { get; set; }

    public override int Id
    {
        get => GenderId;
        set => GenderId = value;
    }

    public string GenderName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}