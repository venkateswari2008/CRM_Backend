using CRM.Api.Common;
using CRM.Api.Extensions;
using CRM.Application.Common;
using CRM.Application.Customers.Dtos;
using CRM.Application.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List(
        [FromQuery] CustomerFilter filter, CancellationToken ct)
    {
        var result = await _customers.ListAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Get([FromRoute] int id, CancellationToken ct)
    {
        var result = await _customers.GetByIdAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customers.CreateAsync(request, ct);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
    {
        var result = await _customers.UpdateAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>Legacy POST update preserved for compatibility with the case-study spec.</summary>
    [HttpPost("{id:int}/update")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> UpdateLegacy(
        [FromRoute] int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct) => await Update(id, request, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var result = await _customers.DeleteAsync(id, ct);
        return result.ToNoContentResult(this);
    }
}
