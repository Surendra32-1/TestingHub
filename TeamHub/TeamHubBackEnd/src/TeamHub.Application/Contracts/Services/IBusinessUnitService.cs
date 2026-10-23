using TeamHub.Application.DTOs.BusinessUnit;

namespace TeamHub.Application.Contracts.Services;

public interface IBusinessUnitService
{
    Task<IReadOnlyList<GetBusinessUnitDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetBusinessUnitDto?> GetByIdAsync(int businessUnitId, CancellationToken cancellationToken = default);
    Task<GetBusinessUnitDto> CreateAsync(CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int businessUnitId, CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int businessUnitId, CancellationToken cancellationToken = default);
}