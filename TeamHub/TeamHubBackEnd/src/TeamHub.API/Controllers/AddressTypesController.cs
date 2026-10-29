using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Auth.Authorization;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.AddressType;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/address-types")]
[Authorize(Policy = RbacPolicies.MasterAddressType)]
public class AddressTypesController(IAddressTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GetAddressTypeDto>>>> GetAll(
      [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
      var items = await service.GetAllAsync(cancellationToken);
      return Ok(new ApiResponse<PagedResult<GetAddressTypeDto>>
      {
        Success = true,
        Message = "Address types retrieved successfully.",
        Data = PagedResult<GetAddressTypeDto>.Create(items, pageNumber, pageSize)
      });
    }

    [HttpGet("{addressTypeId:int}")]
    public async Task<ActionResult<ApiResponse<GetAddressTypeDto>>> GetById(int addressTypeId, CancellationToken cancellationToken)
    {
      var item = await service.GetByIdAsync(addressTypeId, cancellationToken);
      return item is null ? NotFound(NotFoundProblem(addressTypeId)) : Ok(new ApiResponse<GetAddressTypeDto> { Success = true, Message = "Address type retrieved successfully.", Data = item });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetAddressTypeDto>>> Create(CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { addressTypeId = item.AddressTypeId }, new ApiResponse<GetAddressTypeDto> { Success = true, Message = "Address type created successfully.", Data = item });
    }

    [HttpPut("{addressTypeId:int}")]
    public async Task<IActionResult> Update(int addressTypeId, CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken) =>
     await service.UpdateAsync(addressTypeId, request, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(addressTypeId));

    [HttpDelete("{addressTypeId:int}")]
    public async Task<IActionResult> Delete(int addressTypeId, CancellationToken cancellationToken) =>
    await service.DeleteAsync(addressTypeId, cancellationToken) ? NoContent() : NotFound(NotFoundProblem(addressTypeId));

    private ProblemDetails NotFoundProblem(int id) => ApiProblemDetailsFactory.NotFound(
      "Address type not found",
      $"Address type with ID {id} was not found.",
      HttpContext.Request.Path);
}