using TeamHub.Application.DTOs.DressSize;

namespace TeamHub.Application.Contracts.Services;

public interface IDressSizeService
{
    Task<IReadOnlyList<GetDressSizeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetDressSizeDto?> GetByIdAsync(int dressId, CancellationToken cancellationToken = default);
    Task<GetDressSizeDto> CreateAsync(CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int dressId, CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int dressId, CancellationToken cancellationToken = default);
}