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
    [HttpGet("{employeeId:int}")]
    [AllowAnonymous]
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

    [HttpGet("{employeeId:int}/details")]
    [AllowAnonymous]
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

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<GetGridEmployeeDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string searchField = "all",
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetAllAsync(cancellationToken, searchTerm, searchField);

        return Ok(new ApiResponse<PagedResult<GetGridEmployeeDto>>
        {
            Success = true,
            Message = "Employees retrieved successfully.",
            Data = PagedResult<GetGridEmployeeDto>.Create(employees, pageNumber, pageSize)
        });
    }

    [HttpGet("department/{departmentId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<GetGridEmployeeDto>>>> GetActiveByDepartmentId(
        int departmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var employees = await service.GetActiveByDepartmentIdAsync(departmentId, cancellationToken);

        return Ok(new ApiResponse<PagedResult<GetGridEmployeeDto>>
        {
            Success = true,
            Message = "Active department employees retrieved successfully.",
            Data = PagedResult<GetGridEmployeeDto>.Create(employees, pageNumber, pageSize)
        });
    }

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
    [AllowAnonymous]
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