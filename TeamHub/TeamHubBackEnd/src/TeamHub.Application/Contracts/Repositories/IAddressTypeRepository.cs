using TeamHub.Application.DTOs.AddressType;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IAddressTypeRepository
{
    Task<IReadOnlyList<GetAddressTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetAddressTypeDto?> GetByIdAsync(int addressTypeId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(AddressType addressType, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(AddressType addressType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int addressTypeId, CancellationToken cancellationToken = default);
}