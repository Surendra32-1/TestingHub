using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.DressSize;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/dress-sizes")]
[Authorize(Policy = RbacPolicies.MasterDressSize)]
public sealed class DressSizesController(IDressSizeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetDressSizeDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<PagedResult<GetDressSizeDto>>
        {
            Success = true,
            Message = "Dress sizes retrieved successfully.",
            Data = PagedResult<GetDressSizeDto>.Create(await service.GetAllAsync(cancellationToken), pageNumber, pageSize)
        });

    [HttpGet("{dressId:int}")]
    public async Task<ActionResult<ApiResponse<GetDressSizeDto>>> GetById(int dressId, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(dressId, cancellationToken);
        return item is null
            ? NotFound(NotFoundProblem(dressId))
            : Ok(new ApiResponse<GetDressSizeDto> { Success = true, Message = "Dress size retrieved successfully.", Data = item });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetDressSizeDto>>> Create(CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { dressId = item.DressId },
            new ApiResponse<GetDressSizeDto> { Success = true, Message = "Dress size created successfully.", Data = item });
    }

    [HttpPut("{dressId:int}")]
    public async Task<IActionResult> Update(int dressId, CreateOrUpdateDressSizeRequest request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(dressId, request, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(dressId));

    [HttpDelete("{dressId:int}")]
    public async Task<IActionResult> Delete(int dressId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(dressId, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(dressId));

    private ProblemDetails NotFoundProblem(int id) => ApiProblemDetailsFactory.NotFound(
        "Dress size not found", $"Dress size with ID {id} was not found.", HttpContext.Request.Path);
}