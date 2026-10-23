using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityDesignation;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterCotivityDesignation)]
public class CotivityDesignationsController(ICotivityDesignationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetCotivityDesignationDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<PagedResult<GetCotivityDesignationDto>>
        {
            Success = true,
            Message = "Cotivity designations retrieved successfully.",
            Data = PagedResult<GetCotivityDesignationDto>.Create(await service.GetAllAsync(cancellationToken), pageNumber, pageSize)
        });

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GetCotivityDesignationDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound(NotFoundDetails(id)) : Ok(new ApiResponse<GetCotivityDesignationDto>
        {
            Success = true,
            Message = "Cotivity designation retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetCotivityDesignationDto>>> Create(CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.CotivityDesignationId }, new ApiResponse<GetCotivityDesignationDto>
        {
            Success = true,
            Message = "Cotivity designation created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateOrUpdateCotivityDesignationRequest request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound(NotFoundDetails(id));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(NotFoundDetails(id));

    private ProblemDetails NotFoundDetails(int id) => ApiProblemDetailsFactory.NotFound(
        "Cotivity designation not found", $"Cotivity designation with ID {id} was not found.", HttpContext.Request.Path);
}