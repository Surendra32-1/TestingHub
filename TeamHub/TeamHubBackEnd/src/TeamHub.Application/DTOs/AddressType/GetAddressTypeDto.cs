namespace TeamHub.Application.DTOs.AddressType;

public class GetAddressTypeDto
{
    public int AddressTypeId { get; set; }

    public string AddressTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}