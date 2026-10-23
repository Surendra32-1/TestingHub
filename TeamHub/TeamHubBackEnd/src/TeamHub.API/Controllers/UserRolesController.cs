using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Auth.Authorization;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/user-roles")]
[Authorize(Policy = RbacPolicies.AssignUserRoles)]
public class UserRolesController(IUserRoleMatrixService service) : ControllerBase
{
    [HttpGet("matrix")]
    public async Task<ActionResult<ApiResponse<EmployeeRoleMatrixDto>>> GetMatrix([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var matrix = await service.GetMatrixAsync(pageNumber, pageSize, search, ct);
        return Ok(new ApiResponse<EmployeeRoleMatrixDto>
        {
            Success = true,
            Message = "Employee role matrix retrieved successfully.",
            Data = matrix
        });
    }

    [HttpPut("matrix")]
    public async Task<ActionResult<ApiResponse<object?>>> SaveMatrix(SaveUserRolesRequest request, CancellationToken ct)
    {
        await service.SaveAsync(request, User.RoleNames(), User.IsSuperAdmin(), ct);
        return Ok(new ApiResponse<object?>
        {
            Success = true,
            Message = "Role assignments saved successfully."
        });
    }
}
