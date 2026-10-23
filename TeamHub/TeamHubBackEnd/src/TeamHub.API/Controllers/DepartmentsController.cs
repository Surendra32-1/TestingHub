using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Department;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterDepartment)]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<GetDepartmentDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var departments = await departmentService.GetAllAsync(cancellationToken);

        return Ok(new ApiResponse<PagedResult<GetDepartmentDto>>
        {
            Success = true,
            Message = "Departments retrieved successfully.",
            Data = PagedResult<GetDepartmentDto>.Create(departments, pageNumber, pageSize)
        });
    }

    [HttpGet("{departmentId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GetDepartmentDto>>> GetById(int departmentId,CancellationToken cancellationToken)
    {
        var department = await departmentService.GetByIdAsync(departmentId, cancellationToken);

        if (department is null)
        {
            return NotFound(CreateNotFoundProblemDetails(departmentId));
        }

        return Ok(new ApiResponse<GetDepartmentDto>
        {
            Success = true,
            Message = "Department retrieved successfully.",
            Data = department
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetDepartmentDto>>> Create(CreateOrUpdateDepartmentRequest request,CancellationToken cancellationToken)
    {
        var department = await departmentService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { departmentId = department.DepartmentId }, new ApiResponse<GetDepartmentDto>
        {
            Success = true,
            Message = "Department created successfully.",
            Data = department
        });
    }

    [HttpPut("{departmentId:int}")]
    public async Task<IActionResult> Update(int departmentId,CreateOrUpdateDepartmentRequest request,CancellationToken cancellationToken)
    {
        var updated = await departmentService.UpdateAsync(departmentId, request, cancellationToken);

        return updated ? NoContent() : NotFound(CreateNotFoundProblemDetails(departmentId));
    }

    [HttpDelete("{departmentId:int}")]
    public async Task<IActionResult> Delete(int departmentId,CancellationToken cancellationToken)
    {
        var deleted = await departmentService.DeleteAsync(departmentId, cancellationToken);

        return deleted ? NoContent() : NotFound(CreateNotFoundProblemDetails(departmentId));
    }

    private ProblemDetails CreateNotFoundProblemDetails(int departmentId) => ApiProblemDetailsFactory.NotFound(
        "Department not found",
        $"Department with ID {departmentId} was not found.",
        HttpContext.Request.Path);
}