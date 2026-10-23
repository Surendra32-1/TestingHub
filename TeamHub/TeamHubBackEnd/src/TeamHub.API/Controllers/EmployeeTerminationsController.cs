using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Auth.Authorization;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employee-terminations")]
public sealed class EmployeeTerminationsController(IEmployeeTerminationService service) : ControllerBase
{
    [Authorize(Policy = RbacPolicies.TerminationView)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeTerminationResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var records = await service.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<EmployeeTerminationResponse>>
        {
            Success = true,
            Message = "Employee termination records retrieved successfully.",
            Data = records
        });
    }

    [Authorize(Policy = RbacPolicies.TerminationView)]
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeTerminationResponse>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string? search = null,
        [FromQuery] string? department = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetPagedAsync(pageNumber, pageSize, search, department, status, cancellationToken);
        return Ok(new ApiResponse<PagedResult<EmployeeTerminationResponse>>
        {
            Success = true,
            Message = "Employee termination records retrieved successfully.",
            Data = result
        });
    }

    [Authorize(Policy = RbacPolicies.TerminationView)]
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeTerminationResponse>>>> GetByEmployeeId(
        int employeeId, CancellationToken cancellationToken)
    {
        var records = await service.GetByEmployeeIdAsync(employeeId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<EmployeeTerminationResponse>>
        {
            Success = true,
            Message = "Employee termination records retrieved successfully.",
            Data = records
        });
    }

    [Authorize(Policy = RbacPolicies.TerminationView)]
    [HttpGet("{terminationId:int:min(1)}")]
    public async Task<ActionResult<ApiResponse<EmployeeTerminationResponse>>> GetById(
        int terminationId, CancellationToken cancellationToken)
    {
        var record = await service.GetByIdAsync(terminationId, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Termination record not found",
                $"Termination record {terminationId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeTerminationResponse>
            {
                Success = true,
                Message = "Employee termination record retrieved successfully.",
                Data = record
            });
    }

    [Authorize(Policy = RbacPolicies.TerminationAdd)]
    [HttpPost("employee/{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<EmployeeTerminationResponse>>> Create(
        int employeeId, EmployeeTerminationRequest request, CancellationToken cancellationToken)
    {
        var record = await service.CreateAsync(employeeId, request, cancellationToken);
        if (record is null)
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
        }

        return CreatedAtAction(nameof(GetById), new { terminationId = record.TerminationId },
            new ApiResponse<EmployeeTerminationResponse>
            {
                Success = true,
                Message = "Employee termination record created successfully.",
                Data = record
            });
    }

    [Authorize(Policy = RbacPolicies.TerminationManage)]
    [HttpPut("{terminationId:int:min(1)}")]
    public async Task<IActionResult> Update(
        int terminationId, EmployeeTerminationRequest request, CancellationToken cancellationToken)
    {
        if (!await service.UpdateAsync(terminationId, request, cancellationToken))
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Termination record not found",
                $"Termination record {terminationId} was not found.",
                HttpContext.Request.Path));
        }

        return NoContent();
    }

    [Authorize(Policy = RbacPolicies.TerminationManage)]
    [HttpPost("{terminationId:int:min(1)}/status")]
    public async Task<ActionResult<ApiResponse<EmployeeTerminationResponse>>> TransitionStatus(
        int terminationId, UpdateTerminationStatusRequest request, CancellationToken cancellationToken)
    {
        var record = await service.TransitionStatusAsync(terminationId, request.Status, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Termination record not found",
                $"Termination record {terminationId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeTerminationResponse>
            {
                Success = true,
                Message = $"Termination status updated to '{request.Status}'.",
                Data = record
            });
    }

    [Authorize(Policy = RbacPolicies.TerminationManage)]
    [HttpPut("{terminationId:int:min(1)}/clearance")]
    public async Task<ActionResult<ApiResponse<EmployeeTerminationResponse>>> UpdateClearance(
        int terminationId, UpdateTerminationClearanceRequest request, CancellationToken cancellationToken)
    {
        var record = await service.UpdateClearanceAsync(terminationId, request, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Termination record not found",
                $"Termination record {terminationId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeTerminationResponse>
            {
                Success = true,
                Message = "Termination clearance updated successfully.",
                Data = record
            });
    }
}
