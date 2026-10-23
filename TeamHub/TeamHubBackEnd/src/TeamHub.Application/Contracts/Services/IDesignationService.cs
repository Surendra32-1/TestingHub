using TeamHub.Application.DTOs.Designation;

namespace TeamHub.Application.Contracts.Services;

public interface IDesignationService
{
    Task<IReadOnlyList<GetDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetDesignationDto?> GetByIdAsync(int designationId, CancellationToken cancellationToken = default);

    Task<GetDesignationDto> CreateAsync(CreateOrUpdateDesignationRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int designationId, CreateOrUpdateDesignationRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int designationId, CancellationToken cancellationToken = default);
}