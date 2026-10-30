using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Auth.Authorization;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/employee-search")]
[Authorize]
public class EmployeeSearchController(IEmployeeSearchService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = RbacPolicies.PeopleSearchView)]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeSearchCardDto>>>> Search(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? name = null,
        [FromQuery] string? designation = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? contact = null,
        CancellationToken cancellationToken = default)
    {
        var employees = await service.SearchActiveAsync(
            new EmployeeSearchFilter
            {
                Name = name,
                Designation = designation,
                DepartmentId = departmentId,
                Contact = contact
            },
            cancellationToken);

        return Ok(new ApiResponse<PagedResult<EmployeeSearchCardDto>>
        {
            Success = true,
            Message = "Active employees retrieved successfully.",
            Data = PagedResult<EmployeeSearchCardDto>.Create(employees, pageNumber, pageSize)
        });
    }
}
