using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Auth.Authorization;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/role-permissions")]
[Authorize(Policy = RbacPolicies.ManageRolePermissions)]
public class RolePermissionsController(IRolePermissionMatrixService service) : ControllerBase
{
    [HttpGet("matrix")]
    public async Task<ActionResult<ApiResponse<RolePermissionMatrixDto>>> GetMatrix([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var matrix = await service.GetMatrixAsync(pageNumber, pageSize, search, ct);
        return Ok(new ApiResponse<RolePermissionMatrixDto>
        {
            Success = true,
            Message = "Role permission matrix retrieved successfully.",
            Data = matrix
        });
    }

    [HttpPut("matrix")]
    public async Task<ActionResult<ApiResponse<object?>>> SaveMatrix(SaveRolePermissionsRequest request, CancellationToken ct)
    {
        await service.SaveAsync(request, User.PermissionCodes(), User.IsSuperAdmin(), ct);
        return Ok(new ApiResponse<object?>
        {
            Success = true,
            Message = "Permission assignments saved successfully."
        });
    }
}
