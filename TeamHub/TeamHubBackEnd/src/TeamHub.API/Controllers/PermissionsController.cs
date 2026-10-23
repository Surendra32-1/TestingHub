using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Auth.Authorization;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Permission;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = RbacPolicies.ManagePermissions)]
public class PermissionsController(IPermissionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetPermissionDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var items = await service.GetAllAsync(ct);
        return Ok(new ApiResponse<PagedResult<GetPermissionDto>>
        {
            Success = true,
            Message = "Permissions retrieved successfully.",
            Data = PagedResult<GetPermissionDto>.Create(items, pageNumber, pageSize)
        });
    }

    [HttpGet("{permissionId:int}")]
    public async Task<ActionResult<ApiResponse<GetPermissionDto>>> GetById(int permissionId, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(permissionId, ct);
        return item is null ? NotFound(P(permissionId)) : Ok(new ApiResponse<GetPermissionDto> { Success = true, Message = "Permission retrieved successfully.", Data = item });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetPermissionDto>>> Create(CreateOrUpdatePermissionRequest request, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { permissionId = item.PermissionId }, new ApiResponse<GetPermissionDto> { Success = true, Message = "Permission created successfully.", Data = item });
    }

    [HttpPut("{permissionId:int}")]
    public async Task<IActionResult> Update(int permissionId, CreateOrUpdatePermissionRequest request, CancellationToken ct) =>
        await service.UpdateAsync(permissionId, request, ct) ? NoContent() : NotFound(P(permissionId));

    [HttpPatch("{permissionId:int}/status")]
    public async Task<IActionResult> SetStatus(int permissionId, [FromQuery] bool isActive, CancellationToken ct) =>
        await service.SetActiveAsync(permissionId, isActive, ct) ? NoContent() : NotFound(P(permissionId));

    [HttpDelete("{permissionId:int}")]
    public async Task<IActionResult> Delete(int permissionId, CancellationToken ct) =>
        await service.DeleteAsync(permissionId, ct) ? NoContent() : NotFound(P(permissionId));

    private ProblemDetails P(int id) => ApiProblemDetailsFactory.NotFound(
        "Permission not found",
        $"Permission with ID {id} was not found.",
        HttpContext.Request.Path);
}
