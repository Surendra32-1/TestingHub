using TeamHub.Application.DTOs.CotivityEmployee;

namespace TeamHub.Application.Contracts.Services;

public interface ICotivityEmployeeService
{
    Task<IReadOnlyList<GetCotivityEmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityEmployeeDto?> GetByIdAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default);

    Task<GetCotivityEmployeeDto> CreateAsync(CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int cotivityEmployeeId, CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default);
}