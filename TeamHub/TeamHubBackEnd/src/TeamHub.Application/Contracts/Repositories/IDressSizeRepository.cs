using TeamHub.Application.DTOs.DressSize;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IDressSizeRepository
{
    Task<IReadOnlyList<GetDressSizeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetDressSizeDto?> GetByIdAsync(int dressId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(DressSize dressSize, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(DressSize dressSize, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int dressId, CancellationToken cancellationToken = default);
}