using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.CotivityEmployee;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RbacPolicies.MasterCotivityEmployee)]
public class CotivityEmployeesController(ICotivityEmployeeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetCotivityEmployeeDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<PagedResult<GetCotivityEmployeeDto>>
        {
            Success = true,
            Message = "Cotivity employees retrieved successfully.",
            Data = PagedResult<GetCotivityEmployeeDto>.Create(await service.GetAllAsync(cancellationToken), pageNumber, pageSize)
        });

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GetCotivityEmployeeDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound(NotFoundDetails(id)) : Ok(new ApiResponse<GetCotivityEmployeeDto>
        {
            Success = true,
            Message = "Cotivity employee retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetCotivityEmployeeDto>>> Create(CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.CotivityEmployeeId }, new ApiResponse<GetCotivityEmployeeDto>
        {
            Success = true,
            Message = "Cotivity employee created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateOrUpdateCotivityEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (request.ReportsToCotivityEmployeeId == id)
        {
            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "An employee cannot report to themselves.",
                Instance = HttpContext.Request.Path
            };

            ApiProblemDetailsFactory.AddResponseFields(problem);
            problem.Extensions["errors"] = new Dictionary<string, string[]>
            {
                ["reportsToCotivityEmployeeId"] = ["An employee cannot report to themselves."]
            };

            return BadRequest(problem);
        }

        return await service.UpdateAsync(id, request, cancellationToken)
            ? NoContent()
            : NotFound(NotFoundDetails(id));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(NotFoundDetails(id));

    private ProblemDetails NotFoundDetails(int id) => ApiProblemDetailsFactory.NotFound(
        "Cotivity employee not found", $"Cotivity employee with ID {id} was not found.", HttpContext.Request.Path);
}