using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Shared.Responses;
using TeamHub.Application.DTOs.Employees;
using TeamHub.Application.Features.Employees;
using TeamHub.Auth.Authorization;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(
    IEmployeeService service,
    IValidator<CreateOrUpdateEmployeeRequest> employeeValidator,
    IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<ApiResponse<PagedResult<PublicEmployeeDto>>>> GetPublicDirectory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        [FromQuery] string? departmentName = null,
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetAllAsync(cancellationToken, searchTerm, searchField, departmentName);
        var publicEmployees = employees.Select(MapPublicEmployee);

        return Ok(new ApiResponse<PagedResult<PublicEmployeeDto>>
        {
            Success = true,
            Message = "Public employee directory retrieved successfully.",
            Data = PagedResult<PublicEmployeeDto>.Create(publicEmployees, pageNumber, pageSize)
        });
    }

    [AllowAnonymous]
    [HttpGet("public/department/{departmentId:int}")]
    public async Task<ActionResult<ApiResponse<PagedResult<PublicEmployeeDto>>>> GetPublicDepartmentDirectory(
        int departmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetActiveByDepartmentIdAsync(
            departmentId,
            cancellationToken,
            searchTerm,
            searchField);
        var publicEmployees = employees.Select(MapPublicEmployee);

        return Ok(new ApiResponse<PagedResult<PublicEmployeeDto>>
        {
            Success = true,
            Message = "Public department employee directory retrieved successfully.",
            Data = PagedResult<PublicEmployeeDto>.Create(publicEmployees, pageNumber, pageSize)
        });
    }

    private static PublicEmployeeDto MapPublicEmployee(GetGridEmployeeDto employee) => new(
        employee.Id,
        employee.Name,
        employee.Designation,
        employee.DesignationRank,
        employee.Department,
        employee.ReportsToEmployeeId,
        employee.TeamName,
        employee.ImageBase64);

    public sealed record PublicEmployeeDto(
        int Id,
        string Name,
        string? Designation,
        int DesignationRank,
        string? Department,
        int? ReportsToEmployeeId,
        string? TeamName,
        string? ImageBase64);

    [AllowAnonymous]
    [HttpGet("public/{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<PublicEmployeeDetailsDto>>> GetPublicEmployeeDetails(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await service.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
        }

        return Ok(new ApiResponse<PublicEmployeeDetailsDto>
        {
            Success = true,
            Message = "Public employee details retrieved successfully.",
            Data = new PublicEmployeeDetailsDto(
                employee.EmployeeId,
                employee.ENumber,
                string.Join(' ', new[] { employee.FirstName, employee.MiddleName, employee.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                employee.DesignationName,
                employee.DepartmentName,
                employee.TeamName,
                employee.ReportsToEmployeeName,
                employee.StationNumber,
                employee.Email,
                employee.PhoneNumber ?? employee.MobileNumber,
                employee.ExtensionNumber,
                employee.ImageBase64)
        });
    }

    public sealed record PublicEmployeeDetailsDto(
        int EmployeeId,
        int ENumber,
        string Name,
        string? DesignationName,
        string? DepartmentName,
        string? TeamName,
        string? ReportsToEmployeeName,
        string? StationNumber,
        string Email,
        string? PhoneNumber,
        int ExtensionNumber,
        string? ImageBase64);

    [AllowAnonymous]
    [HttpGet("public/{employeeId:int}/hierarchy")]
    public async Task<ActionResult<ApiResponse<PublicEmployeeHierarchyDto>>> GetPublicEmployeeHierarchy(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var hierarchy = await mediator.Send(new GetEmployeeHierarchyQuery(employeeId), cancellationToken);
        if (hierarchy is null)
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
        }

        return Ok(new ApiResponse<PublicEmployeeHierarchyDto>
        {
            Success = true,
            Message = "Public employee hierarchy retrieved successfully.",
            Data = new PublicEmployeeHierarchyDto(
                hierarchy.EmployeeManagers.Select(person => new PublicEmployeeHierarchyPersonDto(
                    person.EmployeeId,
                    person.ParentId,
                    person.EmployeeName,
                    person.DesignationName,
                    person.DepartmentName,
                    person.ImagePath,
                    person.EmployeeNumber,
                    person.Source,
                    person.Level)).ToList(),
                new PublicEmployeeHierarchyPersonDto(
                    hierarchy.SelectedEmployee.EmployeeId,
                    hierarchy.SelectedEmployee.ParentId,
                    hierarchy.SelectedEmployee.EmployeeName,
                    hierarchy.SelectedEmployee.DesignationName,
                    hierarchy.SelectedEmployee.DepartmentName,
                    hierarchy.SelectedEmployee.ImagePath,
                    hierarchy.SelectedEmployee.EmployeeNumber,
                    hierarchy.SelectedEmployee.Source,
                    hierarchy.SelectedEmployee.Level),
                hierarchy.CotivityHierarchy.Select(person => new PublicCotivityHierarchyPersonDto(
                    person.CotivityEmployeeId,
                    person.ParentId,
                    person.CotivityEmployeeName,
                    person.CotivityDesignationName,
                    person.CotivityDepartmentName,
                    person.ImagePath,
                    person.Source,
                    person.Level)).ToList(),
                hierarchy.ReportingHierarchy.Select(person => new PublicEmployeeHierarchyPersonDto(
                    person.EmployeeId,
                    person.ParentId,
                    person.EmployeeName,
                    person.DesignationName,
                    person.DepartmentName,
                    person.ImagePath,
                    person.EmployeeNumber,
                    person.Source,
                    person.Level)).ToList())
        });
    }

    public sealed record PublicEmployeeHierarchyDto(
        IReadOnlyList<PublicEmployeeHierarchyPersonDto> EmployeeManagers,
        PublicEmployeeHierarchyPersonDto SelectedEmployee,
        IReadOnlyList<PublicCotivityHierarchyPersonDto> CotivityHierarchy,
        IReadOnlyList<PublicEmployeeHierarchyPersonDto> ReportingHierarchy);

    public sealed record PublicEmployeeHierarchyPersonDto(
        int EmployeeId,
        int? ParentId,
        string EmployeeName,
        string? DesignationName,
        string? DepartmentName,
        string? ImagePath,
        int EmployeeNumber,
        string Source,
        int Level);

    public sealed record PublicCotivityHierarchyPersonDto(
        int CotivityEmployeeId,
        int? ParentId,
        string CotivityEmployeeName,
        string? CotivityDesignationName,
        string? CotivityDepartmentName,
        string? ImagePath,
        string Source,
        int Level);

    [HttpGet("{employeeId:int}")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<GetEmployeeDto>>> GetById(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await service.GetByIdAsync(employeeId, cancellationToken);
        return employee is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<GetEmployeeDto>
            {
                Success = true,
                Message = "Employee retrieved successfully.",
                Data = employee
            });
    }

    [HttpGet("active-inactive/{employeeId:int}")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<GetEmployeeDto>>> GetActiveInactiveById(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await service.GetActiveInactiveByIdAsync(employeeId, cancellationToken);
        return employee is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<GetEmployeeDto>
            {
                Success = true,
                Message = "Employee retrieved successfully.",
                Data = employee
            });
    }

    [HttpGet("{employeeId:int}/details")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<GetEmployeeDto>>> GetDetails(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await service.GetByIdAsync(employeeId, cancellationToken);
        return employee is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<GetEmployeeDto>
            {
                Success = true,
                Message = "Employee details retrieved successfully.",
                Data = employee
            });
    }

    [Authorize(Policy = RbacPolicies.PeopleCreate)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create(CreateOrUpdateEmployeeRequest request,CancellationToken cancellationToken)
    {
        var createContext = new ValidationContext<CreateOrUpdateEmployeeRequest>(
            request,
            new PropertyChain(),
            new RulesetValidatorSelector(new[] { "default", "Create" }));
        var createValidation = await employeeValidator.ValidateAsync(createContext, cancellationToken);
        if (!createValidation.IsValid)
        {
            var errors = createValidation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            var message = string.Join(" ", createValidation.Errors.Select(error => error.ErrorMessage).Distinct());
            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = message,
                Instance = HttpContext.Request.Path
            };
            ApiProblemDetailsFactory.AddResponseFields(problem, message);
            problem.Extensions["errors"] = errors;
            return BadRequest(problem);
        }

        var result = await service.CreateAsync(request, cancellationToken);
        return Created($"/api/employees/{result.EmployeeId}", new ApiResponse<int>
        {
            Success = true,
            Message = "Employee created successfully.",
            Data = result.EmployeeId
        });
    }

    [Authorize(Policy = RbacPolicies.PeopleUpdate)]
    [HttpPut("{employeeId:int}")]
    public async Task<IActionResult> Update(
        int employeeId,
        CreateOrUpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await service.UpdateAsync(employeeId, request, cancellationToken);
        return updated
            ? NoContent()
            : NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
    }

    [Authorize(Policy = RbacPolicies.PeopleUpdate)]
    [HttpPut("active-inactive/{employeeId:int}")]
    public async Task<IActionResult> UpdateActiveInactive(
        int employeeId,
        CreateOrUpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await service.UpdateActiveInactiveAsync(employeeId, request, cancellationToken);
        return updated
            ? NoContent()
            : NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
    }

    [Authorize(Policy = RbacPolicies.PeopleDelete)]
    [HttpDelete("{employeeId:int}")]
    public async Task<IActionResult> Delete(int employeeId, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(
            employeeId,
            User.Identity?.Name ?? "System",
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
    }

    [HttpGet]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<PagedResult<GetGridEmployeeDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        [FromQuery] string? departmentName = null,
        [FromQuery] string status = "active",
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetAllAsync(cancellationToken, searchTerm, searchField, departmentName, status);

        return Ok(new ApiResponse<PagedResult<GetGridEmployeeDto>>
        {
            Success = true,
            Message = "Employees retrieved successfully.",
            Data = PagedResult<GetGridEmployeeDto>.Create(employees, pageNumber, pageSize)
        });
    }

    [HttpGet("active-inactive")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<PagedResult<GetActiveInactiveEmployeeDto>>>> GetAllActiveInactive(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        [FromQuery] string? departmentName = null,
        [FromQuery] string status = "all",
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetAllActiveInactiveAsync(cancellationToken, searchTerm, searchField, departmentName, status);

        return Ok(new ApiResponse<PagedResult<GetActiveInactiveEmployeeDto>>
        {
            Success = true,
            Message = "Active and inactive employees retrieved successfully.",
            Data = PagedResult<GetActiveInactiveEmployeeDto>.Create(employees, pageNumber, pageSize)
        });
    }

    [HttpGet("department/{departmentId:int}")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<PagedResult<GetGridEmployeeDto>>>> GetActiveByDepartmentId(
        int departmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetActiveByDepartmentIdAsync(
            departmentId,
            cancellationToken,
            searchTerm,
            searchField);

        return Ok(new ApiResponse<PagedResult<GetGridEmployeeDto>>
        {
            Success = true,
            Message = "Active department employees retrieved successfully.",
            Data = PagedResult<GetGridEmployeeDto>.Create(employees, pageNumber, pageSize)
        });
    }

    [Authorize(Policy = RbacPolicies.PeopleView)]
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentTreeDto>>>> GetOrganizationTree(CancellationToken cancellationToken)
    {
        var tree = await service.GetOrganizationTreeAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<DepartmentTreeDto>>
        {
            Success = true,
            Message = "Employee organization tree retrieved successfully.",
            Data = tree
        });
    }

    [HttpGet("{employeeId:int}/hierarchy")]
    [Authorize(Policy = RbacPolicies.PeopleView)]
    public async Task<ActionResult<ApiResponse<EmployeeHierarchyResponse>>> GetHierarchy(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var hierarchy = await mediator.Send(new GetEmployeeHierarchyQuery(employeeId), cancellationToken);
        return hierarchy is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeHierarchyResponse>
            {
                Success = true,
                Message = "Employee hierarchy retrieved successfully.",
                Data = hierarchy
            });
    }

    [HttpGet("employee-master")]
    public async Task<ActionResult<ApiResponse<EmployeeMastersResponse>>> GetEmployeeMastersAsync(CancellationToken cancellationToken)
    {
        var master = await service.GetEmployeeMastersAsync(cancellationToken);

        return Ok(new ApiResponse<EmployeeMastersResponse>
        {
            Success = true,
            Message = "Employee master data retrieved successfully.",
            Data = master
        });
    }


}
