using TeamHub.Application.DTOs.CotivityDesignation;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Contracts.Repositories;

public interface ICotivityDesignationRepository
{
    Task<IReadOnlyList<GetCotivityDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityDesignationDto?> GetByIdAsync(int cotivityDesignationId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CotivityDesignation designation, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CotivityDesignation designation, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityDesignationId, CancellationToken cancellationToken = default);
}