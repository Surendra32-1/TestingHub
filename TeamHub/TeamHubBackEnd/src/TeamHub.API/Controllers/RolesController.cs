using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Role;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = RbacPolicies.MasterRole)]
public class RolesController(IRoleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetRoleDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetRoleDto>>
        {
            Success = true,
            Message = "Roles retrieved successfully.",
            Data = PagedResult<GetRoleDto>.Create(items, pageNumber, pageSize)
        });
    }
    [HttpGet("{roleId:int}")] public async Task<ActionResult<ApiResponse<GetRoleDto>>> GetById(int roleId, CancellationToken ct) { var item = await service.GetByIdAsync(roleId, ct); return item is null ? NotFound(P(roleId)) : Ok(new ApiResponse<GetRoleDto> { Success = true, Message = "Role retrieved successfully.", Data = item }); }
    [HttpPost] public async Task<ActionResult<ApiResponse<GetRoleDto>>> Create(CreateOrUpdateRoleRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { roleId = item.RoleId }, new ApiResponse<GetRoleDto> { Success = true, Message = "Role created successfully.", Data = item }); }
    [HttpPut("{roleId:int}")] public async Task<IActionResult> Update(int roleId, CreateOrUpdateRoleRequest request, CancellationToken ct) => await service.UpdateAsync(roleId, request, ct) ? NoContent() : NotFound(P(roleId));
    [HttpDelete("{roleId:int}")] public async Task<IActionResult> Delete(int roleId, CancellationToken ct) => await service.DeleteAsync(roleId, ct) ? NoContent() : NotFound(P(roleId));
    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Role not found",
        $"Role with ID {id} was not found.",
        HttpContext.Request.Path);
}