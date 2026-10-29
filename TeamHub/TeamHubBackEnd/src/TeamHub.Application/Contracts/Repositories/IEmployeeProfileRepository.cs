using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeProfileRepository
{
    Task<EmployeeProfileDto?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileSummaryDto?> GetSummaryAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<bool> IsActiveBloodGroupAsync(int bloodGroupId, CancellationToken cancellationToken = default);

    Task<bool> AreSelectableSkillsAsync(
        int employeeId,
        IReadOnlyCollection<int> skillIds,
        CancellationToken cancellationToken = default);

    Task<bool> AreSelectableDressSizesAsync(
        int employeeId,
        IReadOnlyCollection<int> dressIds,
        CancellationToken cancellationToken = default);

    Task AddImageAsync(int employeeId, string imagePath, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int employeeId,
        EmployeeProfilePatch patch,
        CancellationToken cancellationToken = default);
}
