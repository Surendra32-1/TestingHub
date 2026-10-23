using TeamHub.Application.DTOs.Employee;
using TeamHub.Application.DTOs.Employees;


namespace TeamHub.Application.Contracts.Services;

public interface IEmployeeService
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
    Task<GetEmployeeDto> CreateAsync(CreateOrUpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int employeeId, CreateOrUpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken = default);
}