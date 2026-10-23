using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityDepartment;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterCotivityDepartment)]
public class CotivityDepartmentsController(ICotivityDepartmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetCotivityDepartmentDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var items = await service.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<PagedResult<GetCotivityDepartmentDto>>
        {
            Success = true,
            Message = "Cotivity departments retrieved successfully.",
            Data = PagedResult<GetCotivityDepartmentDto>.Create(items, pageNumber, pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GetCotivityDepartmentDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound(NotFoundDetails(id)) : Ok(new ApiResponse<GetCotivityDepartmentDto>
        {
            Success = true,
            Message = "Cotivity department retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetCotivityDepartmentDto>>> Create(CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.CotivityDepartmentId }, new ApiResponse<GetCotivityDepartmentDto>
        {
            Success = true,
            Message = "Cotivity department created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateOrUpdateCotivityDepartmentRequest request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound(NotFoundDetails(id));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(NotFoundDetails(id));

    private ProblemDetails NotFoundDetails(int id) => ApiProblemDetailsFactory.NotFound(
        "Cotivity department not found", $"Cotivity department with ID {id} was not found.", HttpContext.Request.Path);
}