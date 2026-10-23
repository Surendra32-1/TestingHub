using TeamHub.Application.DTOs.CotivityDepartment;

namespace TeamHub.Application.Contracts.Services;

public interface ICotivityDepartmentService
{
    Task<IReadOnlyList<GetCotivityDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityDepartmentDto?> GetByIdAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default);

    Task<GetCotivityDepartmentDto> CreateAsync(CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int cotivityDepartmentId, CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityDepartmentId, CancellationToken cancellationToken = default);
}