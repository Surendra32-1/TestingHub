using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.BusinessUnit;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/business-units")]
[Authorize(Policy = RbacPolicies.MasterBusinessUnit)]
public sealed class BusinessUnitsController(IBusinessUnitService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetBusinessUnitDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<PagedResult<GetBusinessUnitDto>>
        {
            Success = true,
            Message = "Business units retrieved successfully.",
            Data = PagedResult<GetBusinessUnitDto>.Create(await service.GetAllAsync(cancellationToken), pageNumber, pageSize)
        });

    [HttpGet("{businessUnitId:int}")]
    public async Task<ActionResult<ApiResponse<GetBusinessUnitDto>>> GetById(int businessUnitId, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(businessUnitId, cancellationToken);
        return item is null
            ? NotFound(NotFoundProblem(businessUnitId))
            : Ok(new ApiResponse<GetBusinessUnitDto> { Success = true, Message = "Business unit retrieved successfully.", Data = item });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetBusinessUnitDto>>> Create(CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessUnitId = item.BusinessUnitId },
            new ApiResponse<GetBusinessUnitDto> { Success = true, Message = "Business unit created successfully.", Data = item });
    }

    [HttpPut("{businessUnitId:int}")]
    public async Task<IActionResult> Update(int businessUnitId, CreateOrUpdateBusinessUnitRequest request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(businessUnitId, request, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(businessUnitId));

    [HttpDelete("{businessUnitId:int}")]
    public async Task<IActionResult> Delete(int businessUnitId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(businessUnitId, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(businessUnitId));

    private ProblemDetails NotFoundProblem(int id) => ApiProblemDetailsFactory.NotFound(
        "Business unit not found", $"Business unit with ID {id} was not found.", HttpContext.Request.Path);
}