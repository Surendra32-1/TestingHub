using TeamHub.Domain.Entities;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Application.DTOs.Employees;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeRepository
{
    Task<PagedResult<GetGridEmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "active");
    Task<PagedResult<GetGridEmployeeDto>> GetActiveByDepartmentIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all");
    Task<PagedResult<GetActiveInactiveEmployeeDto>> GetAllActiveInactiveAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "all");
    Task<GetEmployeeDto?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<GetEmployeeDto?> GetActiveInactiveByIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default);
    Task<EmployeeHierarchyResponse?> GetHierarchyAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> UpdateActiveInactiveAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int employeeId, string deletedBy, CancellationToken cancellationToken = default);
    Task AssignEmployeeRolesAsync(int employeeId, IReadOnlyCollection<int> roleIds, string? email, string modifiedBy, CancellationToken cancellationToken = default);
    Task<int> CreateImageAsync(EmployeeImage image, CancellationToken cancellationToken = default);
    Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken = default);
}
