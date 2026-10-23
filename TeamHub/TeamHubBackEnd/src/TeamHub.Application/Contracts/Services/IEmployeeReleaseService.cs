using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Contracts.Services;

public interface IEmployeeReleaseService
{
    Task<IReadOnlyList<EmployeeReleaseResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PagedResult<EmployeeReleaseResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, string? releaseType, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeReleaseResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeReleaseResponse?> GetByIdAsync(int releasedId, CancellationToken cancellationToken);
    Task<EmployeeReleaseResponse?> CreateAsync(int employeeId, EmployeeReleaseRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int releasedId, EmployeeReleaseRequest request, CancellationToken cancellationToken);

    Task<EmployeeReleaseResponse?> TransitionStatusAsync(int releasedId, string newStatus, CancellationToken cancellationToken);

    Task<EmployeeReleaseResponse?> UpdateClearanceAsync(int releasedId, UpdateReleaseClearanceRequest request, CancellationToken cancellationToken);
}
