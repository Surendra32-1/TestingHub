namespace TeamHub.Domain.Entities;

public class AddressType : AuditableEntity<int>
{
    public int AddressTypeId { get; set; }

    public override int Id
    {
        get => AddressTypeId;
        set => AddressTypeId = value;
    }

    public string AddressTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}