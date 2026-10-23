using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.JobType;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/job-types")]
[Authorize(Policy = RbacPolicies.MasterJobType)]
public class JobTypesController(IJobTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetJobTypeDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetJobTypeDto>>
        {
            Success = true,
            Message = "Job types retrieved successfully.",
            Data = PagedResult<GetJobTypeDto>.Create(items, pageNumber, pageSize)
        });
    }
    [HttpGet("{jobTypeId:int}")] public async Task<ActionResult<ApiResponse<GetJobTypeDto>>> GetById(int jobTypeId, CancellationToken ct) { var item = await service.GetByIdAsync(jobTypeId, ct); return item is null ? NotFound(P(jobTypeId)) : Ok(new ApiResponse<GetJobTypeDto> { Success = true, Message = "Job type retrieved successfully.", Data = item }); }
    [HttpPost] public async Task<ActionResult<ApiResponse<GetJobTypeDto>>> Create(CreateOrUpdateJobTypeRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { jobTypeId = item.JobTypeId }, new ApiResponse<GetJobTypeDto> { Success = true, Message = "Job type created successfully.", Data = item }); }
    [HttpPut("{jobTypeId:int}")] public async Task<IActionResult> Update(int jobTypeId, CreateOrUpdateJobTypeRequest request, CancellationToken ct) => await service.UpdateAsync(jobTypeId, request, ct) ? NoContent() : NotFound(P(jobTypeId));
    [HttpDelete("{jobTypeId:int}")] public async Task<IActionResult> Delete(int jobTypeId, CancellationToken ct) => await service.DeleteAsync(jobTypeId, ct) ? NoContent() : NotFound(P(jobTypeId));
    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Job type not found",
        $"Job type with ID {id} was not found.",
        HttpContext.Request.Path);
}