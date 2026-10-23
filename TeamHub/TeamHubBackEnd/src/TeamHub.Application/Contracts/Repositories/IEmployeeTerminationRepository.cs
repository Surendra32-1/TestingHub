using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeTerminationRepository
{
    Task<IReadOnlyList<EmployeeTerminationResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PagedResult<EmployeeTerminationResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeTerminationResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeTerminationResponse?> GetByIdAsync(int terminationId, CancellationToken cancellationToken);
    Task<EmployeeTerminationResponse?> CreateAsync(int employeeId, EmployeeTerminationRequest request, string actorName, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int terminationId, EmployeeTerminationRequest request, string actorName, CancellationToken cancellationToken);

    Task<EmployeeTerminationResponse?> TransitionStatusAsync(int terminationId, string status, string actorName, CancellationToken cancellationToken);

    Task<EmployeeTerminationResponse?> UpdateClearanceAsync(int terminationId, UpdateTerminationClearanceRequest request, string actorName, CancellationToken cancellationToken);
}
