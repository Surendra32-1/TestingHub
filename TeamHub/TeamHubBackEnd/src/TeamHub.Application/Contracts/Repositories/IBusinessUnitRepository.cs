using TeamHub.Application.DTOs.BusinessUnit;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IBusinessUnitRepository
{
    Task<IReadOnlyList<GetBusinessUnitDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetBusinessUnitDto?> GetByIdAsync(int businessUnitId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(BusinessUnit businessUnit, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(BusinessUnit businessUnit, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int businessUnitId, CancellationToken cancellationToken = default);
}