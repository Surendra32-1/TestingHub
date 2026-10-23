using TeamHub.Application.DTOs.Gender;

namespace TeamHub.Application.Contracts.Services;

public interface IGenderService
{
    Task<IReadOnlyList<GetGenderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GetGenderDto?> GetByIdAsync(int genderId, CancellationToken cancellationToken = default);
    Task<GetGenderDto> CreateAsync(CreateOrUpdateGenderRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int genderId, CreateOrUpdateGenderRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int genderId, CancellationToken cancellationToken = default);
}