using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Gender;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/genders")]
[Authorize(Policy = RbacPolicies.MasterGender)]
public class GendersController(IGenderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetGenderDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var items = await service.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<PagedResult<GetGenderDto>>
        {
            Success = true,
            Message = "Genders retrieved successfully.",
            Data = PagedResult<GetGenderDto>.Create(items, pageNumber, pageSize)
        });
    }

    [HttpGet("{genderId:int}")]
    public async Task<ActionResult<ApiResponse<GetGenderDto>>> GetById(int genderId, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(genderId, cancellationToken);
        return item is null ? NotFound(NotFoundProblem(genderId)) : Ok(new ApiResponse<GetGenderDto> { Success = true, Message = "Gender retrieved successfully.", Data = item });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetGenderDto>>> Create(CreateOrUpdateGenderRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { genderId = item.GenderId }, new ApiResponse<GetGenderDto> { Success = true, Message = "Gender created successfully.", Data = item });
    }

    [HttpPut("{genderId:int}")]
    public async Task<IActionResult> Update(int genderId, CreateOrUpdateGenderRequest request, CancellationToken cancellationToken) => await service.UpdateAsync(genderId, request, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(genderId));

    [HttpDelete("{genderId:int}")]
    public async Task<IActionResult> Delete(int genderId, CancellationToken cancellationToken) => await service.DeleteAsync(genderId, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(genderId));

    private ProblemDetails NotFoundProblem(int id) => ApiProblemDetailsFactory.NotFound(
        "Gender not found",
        $"Gender with ID {id} was not found.",
        HttpContext.Request.Path);
}