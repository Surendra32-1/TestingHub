using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.BloodGroup;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterBloodGroup)]
public class BloodGroupsController(IBloodGroupService bloodGroupService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetBloodGroupDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var bloodGroups = await bloodGroupService.GetAllAsync(cancellationToken);

        return Ok(new ApiResponse<PagedResult<GetBloodGroupDto>>
        {
            Success = true,
            Message = "Blood groups retrieved successfully.",
            Data = PagedResult<GetBloodGroupDto>.Create(bloodGroups, pageNumber, pageSize)
        });
    }

    [HttpGet("{bloodGroupId:int}")]
    public async Task<ActionResult<ApiResponse<GetBloodGroupDto>>> GetById(int bloodGroupId,CancellationToken cancellationToken)
    {
        var bloodGroup = await bloodGroupService.GetByIdAsync(bloodGroupId, cancellationToken);

        if (bloodGroup is null)
        {
            return NotFound(CreateNotFoundProblemDetails(bloodGroupId));
        }

        return Ok(new ApiResponse<GetBloodGroupDto>
        {
            Success = true,
            Message = "Blood group retrieved successfully.",
            Data = bloodGroup
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetBloodGroupDto>>> Create(CreateOrUpdateBloodGroupRequest request,CancellationToken cancellationToken)
    {
        var bloodGroup = await bloodGroupService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { bloodGroupId = bloodGroup.BloodGroupId },
            new ApiResponse<GetBloodGroupDto>
            {
                Success = true,
                Message = "Blood group created successfully.",
                Data = bloodGroup
            });
    }

    [HttpPut("{bloodGroupId:int}")]
    public async Task<IActionResult> Update(int bloodGroupId,CreateOrUpdateBloodGroupRequest request,CancellationToken cancellationToken)
    {
        var updated = await bloodGroupService.UpdateAsync(bloodGroupId, request, cancellationToken);

        return updated ? NoContent() : NotFound(CreateNotFoundProblemDetails(bloodGroupId));
    }

    [HttpDelete("{bloodGroupId:int}")]
    public async Task<IActionResult> Delete(int bloodGroupId,CancellationToken cancellationToken)
    {
        var deleted = await bloodGroupService.DeleteAsync(bloodGroupId, cancellationToken);

        return deleted ? NoContent() : NotFound(CreateNotFoundProblemDetails(bloodGroupId));
    }

    private ProblemDetails CreateNotFoundProblemDetails(int bloodGroupId) => ApiProblemDetailsFactory.NotFound(
        "Blood group not found",
        $"Blood group with ID {bloodGroupId} was not found.",
        HttpContext.Request.Path);
}