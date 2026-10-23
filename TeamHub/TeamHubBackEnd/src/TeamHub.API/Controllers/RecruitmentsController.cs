using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Auth.Authorization;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/recruitments")]
[Authorize(Policy = RbacPolicies.MasterRecruitment)]
public class RecruitmentsController(IRecruitmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetRecruitmentDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetRecruitmentDto>>
        {
            Success = true,
            Message = "Recruitments retrieved successfully.",
            Data = PagedResult<GetRecruitmentDto>.Create(items, pageNumber, pageSize)
        });
    }
    [HttpGet("{recruitmentId:int}")] public async Task<ActionResult<ApiResponse<GetRecruitmentDto>>> GetById(int recruitmentId, CancellationToken ct) { var item = await service.GetByIdAsync(recruitmentId, ct); return item is null ? NotFound(P(recruitmentId)) : Ok(new ApiResponse<GetRecruitmentDto> { Success = true, Message = "Recruitment retrieved successfully.", Data = item }); }
    [HttpPost] public async Task<ActionResult<ApiResponse<GetRecruitmentDto>>> Create(CreateOrUpdateRecruitmentRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { recruitmentId = item.RecruitmentId }, new ApiResponse<GetRecruitmentDto> { Success = true, Message = "Recruitment created successfully.", Data = item }); }
    [HttpPut("{recruitmentId:int}")] public async Task<IActionResult> Update(int recruitmentId, CreateOrUpdateRecruitmentRequest request, CancellationToken ct) => await service.UpdateAsync(recruitmentId, request, ct) ? NoContent() : NotFound(P(recruitmentId));
    [HttpDelete("{recruitmentId:int}")] public async Task<IActionResult> Delete(int recruitmentId, CancellationToken ct) => await service.DeleteAsync(recruitmentId, ct) ? NoContent() : NotFound(P(recruitmentId));
    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Recruitment not found",
        $"Recruitment with ID {id} was not found.",
        HttpContext.Request.Path);
}