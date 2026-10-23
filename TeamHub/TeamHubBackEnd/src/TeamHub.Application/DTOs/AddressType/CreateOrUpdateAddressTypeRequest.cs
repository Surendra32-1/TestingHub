namespace TeamHub.Application.DTOs.AddressType;

public class CreateOrUpdateAddressTypeRequest
{
    public string AddressTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}