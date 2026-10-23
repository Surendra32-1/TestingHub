using TeamHub.Application.DTOs.CotivityDesignation;

namespace TeamHub.Application.Contracts.Services;

public interface ICotivityDesignationService
{
    Task<IReadOnlyList<GetCotivityDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GetCotivityDesignationDto?> GetByIdAsync(int cotivityDesignationId, CancellationToken cancellationToken = default);

    Task<GetCotivityDesignationDto> CreateAsync(CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int cotivityDesignationId, CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int cotivityDesignationId, CancellationToken cancellationToken = default);
}