namespace TeamHub.Domain.Entities;

public class EmployeeAddress : AuditableEntity<int>
{
    public int AddressId { get; set; }

    public override int Id
    {
        get => AddressId;
        set => AddressId = value;
    }

    public int EmployeeId { get; set; }
    public int AddressTypeId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public Employee? Employee { get; set; }
}