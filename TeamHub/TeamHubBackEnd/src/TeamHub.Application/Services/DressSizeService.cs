using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.DressSize;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class DressSizeService(IDressSizeRepository repository) : IDressSizeService
{
    public Task<IReadOnlyList<GetDressSizeDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetDressSizeDto?> GetByIdAsync(int dressId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(dressId, cancellationToken);

    public async Task<GetDressSizeDto> CreateAsync(CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new DressSize
        {
            DressName = request.DressName.Trim(),
            Size = request.Size.Trim(),
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };
        entity.DressId = await repository.CreateAsync(entity, cancellationToken);
        return new GetDressSizeDto
        {
            DressId = entity.DressId,
            DressName = entity.DressName,
            Size = entity.Size,
            IsActive = entity.IsActive
        };
    }

    public Task<bool> UpdateAsync(int dressId, CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new DressSize
        {
            DressId = dressId,
            DressName = request.DressName.Trim(),
            Size = request.Size.Trim(),
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        }, cancellationToken);

    public Task<bool> DeleteAsync(int dressId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(dressId, cancellationToken);
}