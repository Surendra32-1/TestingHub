using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Application.Workflows;
using TeamHub.Auth.Authorization;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employee-releases")]
public sealed class EmployeeReleasesController(
    IEmployeeReleaseService service,
    IAuthorizationService authorization) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> TransitionPolicies =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ReleaseStatusWorkflow.Submitted] = RbacPolicies.ResignationSubmit,
            [ReleaseStatusWorkflow.PendingApproval] = RbacPolicies.ResignationSendForApproval,
            [ReleaseStatusWorkflow.Approved] = RbacPolicies.ResignationApprove,
            [ReleaseStatusWorkflow.Released] = RbacPolicies.ResignationRelease,
            [ReleaseStatusWorkflow.Rejected] = RbacPolicies.ResignationReject,
            [ReleaseStatusWorkflow.Cancelled] = RbacPolicies.ResignationCancel
        };

    [Authorize(Policy = RbacPolicies.ResignationView)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeReleaseResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var records = await service.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<EmployeeReleaseResponse>>
        {
            Success = true,
            Message = "Employee resignation records retrieved successfully.",
            Data = records
        });
    }

    [Authorize(Policy = RbacPolicies.ResignationView)]
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeReleaseResponse>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string? search = null,
        [FromQuery] string? department = null,
        [FromQuery] string? status = null,
        [FromQuery] string? releaseType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetPagedAsync(pageNumber, pageSize, search, department, status, releaseType, cancellationToken);
        return Ok(new ApiResponse<PagedResult<EmployeeReleaseResponse>>
        {
            Success = true,
            Message = "Employee release records retrieved successfully.",
            Data = result
        });
    }

    [Authorize(Policy = RbacPolicies.ResignationView)]
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeReleaseResponse>>>> GetByEmployeeId(
        int employeeId, CancellationToken cancellationToken)
    {
        var records = await service.GetByEmployeeIdAsync(employeeId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<EmployeeReleaseResponse>>
        {
            Success = true,
            Message = "Employee release records retrieved successfully.",
            Data = records
        });
    }

    [Authorize(Policy = RbacPolicies.ResignationView)]
    [HttpGet("{releasedId:int:min(1)}")]
    public async Task<ActionResult<ApiResponse<EmployeeReleaseResponse>>> GetById(
        int releasedId, CancellationToken cancellationToken)
    {
        var record = await service.GetByIdAsync(releasedId, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Release record not found",
                $"Release record {releasedId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeReleaseResponse>
            {
                Success = true,
                Message = "Employee release record retrieved successfully.",
                Data = record
            });
    }

    [Authorize]
    [HttpPost("employee/{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<EmployeeReleaseResponse>>> Create(
        int employeeId, EmployeeReleaseRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.ReleaseType, ReleaseStatusWorkflow.ResignationType, StringComparison.OrdinalIgnoreCase))
        {
            var addAuth = await authorization.AuthorizeAsync(User, null, RbacPolicies.ResignationAdd);
            if (!addAuth.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiProblemDetailsFactory.Forbidden(
                    "Not permitted",
                    "You do not have permission to file a resignation.",
                    HttpContext.Request.Path));
            }

            if (!TryGetSelfEmployeeId(out var callerEmployeeId) || callerEmployeeId != employeeId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiProblemDetailsFactory.Forbidden(
                    "Resignation is self-service only",
                    "An employee can file a resignation only for themselves. This form cannot be filled on behalf of another employee.",
                    HttpContext.Request.Path));
            }
        }
        else
        {
            var releaseAuth = await authorization.AuthorizeAsync(User, null, RbacPolicies.PeopleRelease);
            if (!releaseAuth.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiProblemDetailsFactory.Forbidden(
                    "Not permitted",
                    "You do not have permission to release an employee.",
                    HttpContext.Request.Path));
            }
        }

        var record = await service.CreateAsync(employeeId, request, cancellationToken);
        if (record is null)
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Employee not found",
                $"Employee with ID {employeeId} was not found.",
                HttpContext.Request.Path));
        }

        return CreatedAtAction(nameof(GetById), new { releasedId = record.ReleasedId },
            new ApiResponse<EmployeeReleaseResponse>
            {
                Success = true,
                Message = "Employee release record created successfully.",
                Data = record
            });
    }

    [Authorize(Policy = RbacPolicies.ResignationAdd)]
    [HttpPut("{releasedId:int:min(1)}")]
    public async Task<IActionResult> Update(
        int releasedId, EmployeeReleaseRequest request, CancellationToken cancellationToken)
    {
        if (!await service.UpdateAsync(releasedId, request, cancellationToken))
        {
            return NotFound(ApiProblemDetailsFactory.NotFound(
                "Release record not found",
                $"Release record {releasedId} was not found.",
                HttpContext.Request.Path));
        }

        return NoContent();
    }

    [HttpPost("{releasedId:int:min(1)}/status")]
    public async Task<ActionResult<ApiResponse<EmployeeReleaseResponse>>> TransitionStatus(
        int releasedId, UpdateReleaseStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TransitionPolicies.TryGetValue(request.Status, out var requiredPolicy))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiProblemDetailsFactory.Forbidden(
                "Action not permitted",
                $"'{request.Status}' is not a permitted resignation action.",
                HttpContext.Request.Path));
        }

        var permission = await authorization.AuthorizeAsync(User, null, requiredPolicy);
        if (!permission.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiProblemDetailsFactory.Forbidden(
                "Action not permitted",
                $"You do not have permission to move a resignation to '{request.Status}'.",
                HttpContext.Request.Path));
        }

        var record = await service.TransitionStatusAsync(releasedId, request.Status, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Release record not found",
                $"Release record {releasedId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeReleaseResponse>
            {
                Success = true,
                Message = $"Resignation status updated to '{request.Status}'.",
                Data = record
            });
    }

    [Authorize(Policy = RbacPolicies.ResignationExitChecklist)]
    [HttpPut("{releasedId:int:min(1)}/clearance")]
    public async Task<ActionResult<ApiResponse<EmployeeReleaseResponse>>> UpdateClearance(
        int releasedId, UpdateReleaseClearanceRequest request, CancellationToken cancellationToken)
    {
        var record = await service.UpdateClearanceAsync(releasedId, request, cancellationToken);
        return record is null
            ? NotFound(ApiProblemDetailsFactory.NotFound(
                "Release record not found",
                $"Release record {releasedId} was not found.",
                HttpContext.Request.Path))
            : Ok(new ApiResponse<EmployeeReleaseResponse>
            {
                Success = true,
                Message = "Resignation clearance updated successfully.",
                Data = record
            });
    }

    private bool TryGetSelfEmployeeId(out int employeeId) =>
        int.TryParse(User.FindFirstValue("EmployeeId"), out employeeId) && employeeId > 0;
}
