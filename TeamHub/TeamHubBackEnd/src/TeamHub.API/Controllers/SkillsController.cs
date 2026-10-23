using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Skill;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/skills")]
[Authorize(Policy = RbacPolicies.MasterSkills)]
public class SkillsController(ISkillService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetSkillDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetSkillDto>>
        {
            Success = true,
            Message = "Skills retrieved successfully.",
            Data = PagedResult<GetSkillDto>.Create(items, pageNumber, pageSize)
        });
    }
    [HttpGet("{skillId:int}")] public async Task<ActionResult<ApiResponse<GetSkillDto>>> GetById(int skillId, CancellationToken ct) { var item = await service.GetByIdAsync(skillId, ct); return item is null ? NotFound(P(skillId)) : Ok(new ApiResponse<GetSkillDto> { Success = true, Message = "Skill retrieved successfully.", Data = item }); }
    [HttpPost] public async Task<ActionResult<ApiResponse<GetSkillDto>>> Create(CreateOrUpdateSkillRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { skillId = item.SkillId }, new ApiResponse<GetSkillDto> { Success = true, Message = "Skill created successfully.", Data = item }); }
    [HttpPut("{skillId:int}")] public async Task<IActionResult> Update(int skillId, CreateOrUpdateSkillRequest request, CancellationToken ct) => await service.UpdateAsync(skillId, request, ct) ? NoContent() : NotFound(P(skillId));
    [HttpDelete("{skillId:int}")] public async Task<IActionResult> Delete(int skillId, CancellationToken ct) => await service.DeleteAsync(skillId, ct) ? NoContent() : NotFound(P(skillId));
    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Skill not found",
        $"Skill with ID {id} was not found.",
        HttpContext.Request.Path);
}