using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeReleaseRepository
{
    Task<IReadOnlyList<EmployeeReleaseResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PagedResult<EmployeeReleaseResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, string? releaseType, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeReleaseResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeReleaseResponse?> GetByIdAsync(int releasedId, CancellationToken cancellationToken);
    Task<EmployeeReleaseResponse?> CreateAsync(int employeeId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int releasedId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken);

    Task<EmployeeReleaseResponse?> TransitionStatusAsync(int releasedId, string status, string actorName, CancellationToken cancellationToken);

    Task<EmployeeReleaseResponse?> UpdateClearanceAsync(int releasedId, UpdateReleaseClearanceRequest request, string actorName, CancellationToken cancellationToken);
}
