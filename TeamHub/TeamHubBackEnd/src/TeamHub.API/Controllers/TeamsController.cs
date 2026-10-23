using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Team;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Policy = RbacPolicies.MasterTeam)]
public class TeamsController(ITeamService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetTeamDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetTeamDto>>
        {
            Success = true,
            Message = "Teams retrieved successfully.",
            Data = PagedResult<GetTeamDto>.Create(items, pageNumber, pageSize)
        });
    }
    [HttpGet("{teamId:int}")] public async Task<ActionResult<ApiResponse<GetTeamDto>>> GetById(int teamId, CancellationToken ct) { var item = await service.GetByIdAsync(teamId, ct); return item is null ? NotFound(P(teamId)) : Ok(new ApiResponse<GetTeamDto> { Success = true, Message = "Team retrieved successfully.", Data = item }); }
    [HttpPost] public async Task<ActionResult<ApiResponse<GetTeamDto>>> Create(CreateOrUpdateTeamRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { teamId = item.TeamId }, new ApiResponse<GetTeamDto> { Success = true, Message = "Team created successfully.", Data = item }); }
    [HttpPut("{teamId:int}")] public async Task<IActionResult> Update(int teamId, CreateOrUpdateTeamRequest request, CancellationToken ct) => await service.UpdateAsync(teamId, request, ct) ? NoContent() : NotFound(P(teamId));
    [HttpDelete("{teamId:int}")] public async Task<IActionResult> Delete(int teamId, CancellationToken ct) => await service.DeleteAsync(teamId, ct) ? NoContent() : NotFound(P(teamId));
    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Team not found",
        $"Team with ID {id} was not found.",
        HttpContext.Request.Path);
}