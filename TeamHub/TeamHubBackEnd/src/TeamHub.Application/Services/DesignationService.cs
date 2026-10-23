using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class DesignationService(IDesignationRepository repository) : IDesignationService
{
    public Task<IReadOnlyList<GetDesignationDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetDesignationDto?> GetByIdAsync(int designationId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(designationId, cancellationToken);

    public async Task<GetDesignationDto> CreateAsync(CreateOrUpdateDesignationRequest request,CancellationToken cancellationToken = default)
    {
        var designation = new Designation
        {
            DesignationName = request.DesignationName,
            Rank = request.Rank,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        designation.DesignationId = await repository.CreateAsync(designation, cancellationToken);

        return new GetDesignationDto
        {
            DesignationId = designation.DesignationId,
            DesignationName = designation.DesignationName,
            Rank = designation.Rank,
            IsActive = designation.IsActive
        };
    }

    public Task<bool> UpdateAsync(int designationId,CreateOrUpdateDesignationRequest request,CancellationToken cancellationToken = default)
    {
        var designation = new Designation
        {
            DesignationId = designationId,
            DesignationName = request.DesignationName,
            Rank = request.Rank,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        };

        return repository.UpdateAsync(designation, cancellationToken);
    }

    public Task<bool> DeleteAsync(int designationId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(designationId, cancellationToken);
}