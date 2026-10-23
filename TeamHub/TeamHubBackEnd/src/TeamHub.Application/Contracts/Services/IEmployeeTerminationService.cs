using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Contracts.Services;

public interface IEmployeeTerminationService
{
    Task<IReadOnlyList<EmployeeTerminationResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PagedResult<EmployeeTerminationResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeTerminationResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeTerminationResponse?> GetByIdAsync(int terminationId, CancellationToken cancellationToken);
    Task<EmployeeTerminationResponse?> CreateAsync(int employeeId, EmployeeTerminationRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int terminationId, EmployeeTerminationRequest request, CancellationToken cancellationToken);

    Task<EmployeeTerminationResponse?> TransitionStatusAsync(int terminationId, string newStatus, CancellationToken cancellationToken);

    Task<EmployeeTerminationResponse?> UpdateClearanceAsync(int terminationId, UpdateTerminationClearanceRequest request, CancellationToken cancellationToken);
}
