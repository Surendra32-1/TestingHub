using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityEmployee;
using TeamHub.Domain.Entities;
using TeamHub.Shared.Common.Services;

namespace TeamHub.Application.Services;

public sealed class CotivityEmployeeService(ICotivityEmployeeRepository repository) : ICotivityEmployeeService
{
    public Task<IReadOnlyList<GetCotivityEmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<GetCotivityEmployeeDto?> GetByIdAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(cotivityEmployeeId, cancellationToken);

    public async Task<GetCotivityEmployeeDto> CreateAsync(CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = new CotivityEmployee
        {
            CotivityEmployeeName = request.CotivityEmployeeName,
            CotivityDepartmentId = request.CotivityDepartmentId,
            CotivityDepartmentDesignationId = request.CotivityDepartmentDesignationId,
            ReportsToCotivityEmployeeId = request.ReportsToCotivityEmployeeId,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        employee.CotivityEmployeeId = await repository.CreateAsync(employee, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var imageFileName = $"cotivity-employee-{employee.CotivityEmployeeId}-{Guid.NewGuid():N}";
            var imagePath = request.ImageBase64.ToFileUrl(FolderName.Cotivity_Employee_FOLDER, imageFileName);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                await repository.CreateImageAsync(new CotivityEmployeeImage
                {
                    CotivityEmployeeId = employee.CotivityEmployeeId,
                    CotivityImagePath = imagePath,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }, cancellationToken);
            }
        }

        return new GetCotivityEmployeeDto
        {
            CotivityEmployeeId = employee.CotivityEmployeeId,
            CotivityEmployeeName = employee.CotivityEmployeeName,
            CotivityDepartmentId = employee.CotivityDepartmentId,
            CotivityDepartmentDesignationId = employee.CotivityDepartmentDesignationId,
            ReportsToCotivityEmployeeId = employee.ReportsToCotivityEmployeeId,
            IsActive = employee.IsActive
        };
    }

    public async Task<bool> UpdateAsync(int cotivityEmployeeId, CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var updated = await repository.UpdateAsync(new CotivityEmployee
        {
            CotivityEmployeeId = cotivityEmployeeId,
            CotivityEmployeeName = request.CotivityEmployeeName,
            CotivityDepartmentId = request.CotivityDepartmentId,
            CotivityDepartmentDesignationId = request.CotivityDepartmentDesignationId,
            ReportsToCotivityEmployeeId = request.ReportsToCotivityEmployeeId,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System"
        }, cancellationToken);

        if (!updated || string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return updated;
        }

        var imageFileName = $"cotivity-employee-{cotivityEmployeeId}-{Guid.NewGuid():N}";
        var imagePath = request.ImageBase64.ToFileUrl(FolderName.Cotivity_Employee_FOLDER, imageFileName);
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            await repository.CreateImageAsync(new CotivityEmployeeImage
            {
                CotivityEmployeeId = cotivityEmployeeId,
                CotivityImagePath = imagePath,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }, cancellationToken);
        }

        return true;
    }

    public Task<bool> DeleteAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(cotivityEmployeeId, cancellationToken);
}