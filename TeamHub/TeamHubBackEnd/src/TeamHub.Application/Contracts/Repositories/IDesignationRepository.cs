using TeamHub.Application.DTOs.Designation;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface IDesignationRepository
{
    Task<IReadOnlyList<GetDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetDesignationDto?> GetByIdAsync(int designationId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Designation designation, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Designation designation, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int designationId, CancellationToken cancellationToken = default);
}