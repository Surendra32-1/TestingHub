using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Contracts.Services;

public interface IEmployeeProfileService
{
    Task<EmployeeProfileDto?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileSummaryDto?> GetSummaryAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileUpdateResult> UpdateAsync(
        int employeeId,
        EmployeeProfilePatch patch,
        CancellationToken cancellationToken = default);
}
