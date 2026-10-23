using TeamHub.Domain.Entities;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Application.DTOs.Employees;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<GetGridEmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        string? searchTerm = null,
        string searchField = "all");
    Task<IReadOnlyList<GetGridEmployeeDto>> GetActiveByDepartmentIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default);
    Task<GetEmployeeDto?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default);
    Task<EmployeeHierarchyResponse?> GetHierarchyAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task AssignEmployeeRolesAsync(int employeeId, IReadOnlyCollection<int> roleIds, string? email, string modifiedBy, CancellationToken cancellationToken = default);
    Task<int> CreateImageAsync(EmployeeImage image, CancellationToken cancellationToken = default);
    Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken = default);
}