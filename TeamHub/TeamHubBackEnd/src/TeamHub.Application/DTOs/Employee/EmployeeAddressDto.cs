namespace TeamHub.Application.DTOs.Employee;

public class EmployeeAddressDto
{
    public int AddressId { get; set; }
    public int AddressTypeId { get; set; }
    public string AddressTypeName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsPrimary { get; set; }
}