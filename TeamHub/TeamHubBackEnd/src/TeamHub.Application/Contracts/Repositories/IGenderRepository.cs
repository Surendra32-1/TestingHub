using TeamHub.Application.DTOs.Gender;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IGenderRepository
{
    Task<IReadOnlyList<GetGenderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetGenderDto?> GetByIdAsync(int genderId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Gender gender, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Gender gender, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int genderId, CancellationToken cancellationToken = default);
}