using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityDesignation;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class CotivityDesignationService(ICotivityDesignationRepository repository) : ICotivityDesignationService
{
    public Task<IReadOnlyList<GetCotivityDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetCotivityDesignationDto?> GetByIdAsync(int cotivityDesignationId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(cotivityDesignationId, cancellationToken);

    public async Task<GetCotivityDesignationDto> CreateAsync(CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken = default)
    {
        var designation = new CotivityDesignation
        {
            CotivityDesignationName = request.CotivityDesignationName,
            CotivityRank = request.CotivityRank,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        designation.CotivityDesignationId = await repository.CreateAsync(designation, cancellationToken);

        return new GetCotivityDesignationDto
        {
            CotivityDesignationId = designation.CotivityDesignationId,
            CotivityDesignationName = designation.CotivityDesignationName,
            CotivityRank = designation.CotivityRank,
            IsActive = designation.IsActive
        };
    }

    public Task<bool> UpdateAsync(int cotivityDesignationId, CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new CotivityDesignation
        {
            CotivityDesignationId = cotivityDesignationId,
            CotivityDesignationName = request.CotivityDesignationName,
            CotivityRank = request.CotivityRank,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        }, cancellationToken);

    public Task<bool> DeleteAsync(int cotivityDesignationId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(cotivityDesignationId, cancellationToken);
}