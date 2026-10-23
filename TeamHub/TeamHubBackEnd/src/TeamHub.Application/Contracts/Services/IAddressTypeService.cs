using TeamHub.Application.DTOs.AddressType;

namespace TeamHub.Application.Contracts.Services;

public interface IAddressTypeService
{
    Task<IReadOnlyList<GetAddressTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetAddressTypeDto?> GetByIdAsync(int addressTypeId, CancellationToken cancellationToken = default);
    Task<GetAddressTypeDto> CreateAsync(CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int addressTypeId, CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int addressTypeId, CancellationToken cancellationToken = default);
}