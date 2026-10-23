using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterDesignation)]
public class DesignationsController(IDesignationService designationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetDesignationDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var designations = await designationService.GetAllAsync(cancellationToken);

        return Ok(new ApiResponse<PagedResult<GetDesignationDto>>
        {
            Success = true,
            Message = "Designations retrieved successfully.",
            Data = PagedResult<GetDesignationDto>.Create(designations, pageNumber, pageSize)
        });
    }

    [HttpGet("{designationId:int}")]
    public async Task<ActionResult<ApiResponse<GetDesignationDto>>> GetById(int designationId,CancellationToken cancellationToken)
    {
        var designation = await designationService.GetByIdAsync(designationId, cancellationToken);

        if (designation is null)
        {
            return NotFound(CreateNotFoundProblemDetails(designationId));
        }

        return Ok(new ApiResponse<GetDesignationDto>
        {
            Success = true,
            Message = "Designation retrieved successfully.",
            Data = designation
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetDesignationDto>>> Create( CreateOrUpdateDesignationRequest request,CancellationToken cancellationToken)
    {
        var designation = await designationService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { designationId = designation.DesignationId },
            new ApiResponse<GetDesignationDto>
            {
                Success = true,
                Message = "Designation created successfully.",
                Data = designation
            });
    }

    [HttpPut("{designationId:int}")]
    public async Task<IActionResult> Update(int designationId,CreateOrUpdateDesignationRequest request,CancellationToken cancellationToken)
    {
        var updated = await designationService.UpdateAsync(designationId, request, cancellationToken);

        return updated ? NoContent() : NotFound(CreateNotFoundProblemDetails(designationId));
    }

    [HttpDelete("{designationId:int}")]
    public async Task<IActionResult> Delete(int designationId,CancellationToken cancellationToken)
    {
        var deleted = await designationService.DeleteAsync(designationId, cancellationToken);

        return deleted ? NoContent() : NotFound(CreateNotFoundProblemDetails(designationId));
    }

    private ProblemDetails CreateNotFoundProblemDetails(int designationId) => ApiProblemDetailsFactory.NotFound(
        "Designation not found",
        $"Designation with ID {designationId} was not found.",
        HttpContext.Request.Path);
}