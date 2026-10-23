using TeamHub.Application.DTOs.CotivityEmployee;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface ICotivityEmployeeRepository
{
    Task<IReadOnlyList<GetCotivityEmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityEmployeeDto?> GetByIdAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CotivityEmployee employee, CancellationToken cancellationToken = default);

    Task<int> CreateImageAsync(CotivityEmployeeImage image, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CotivityEmployee employee, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default);
}