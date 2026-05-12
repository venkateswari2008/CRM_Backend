using CRM.Api.Extensions;
using CRM.Application.Common;
using CRM.Application.Sales.Dtos;
using CRM.Application.Sales.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
[Produces("application/json")]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _sales;

    public SalesController(ISaleService sales) => _sales = sales;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SaleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SaleDto>>> List(
        [FromQuery] SaleFilter filter, CancellationToken ct)
    {
        var result = await _sales.ListAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> Get([FromRoute] int id, CancellationToken ct)
    {
        var result = await _sales.GetByIdAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SaleDto>> Create(
        [FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        var result = await _sales.CreateAsync(request, ct);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SaleDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken ct)
    {
        var result = await _sales.UpdateAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<SaleDto>> UpdateLegacy(
        [FromRoute] int id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken ct) => await Update(id, request, ct);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var result = await _sales.DeleteAsync(id, ct);
        return result.ToNoContentResult(this);
    }

    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export([FromQuery] SaleFilter filter, CancellationToken ct)
    {
        var result = await _sales.ExportCsvAsync(filter, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);

        var fileName = $"sales-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(result.Value!, "text/csv", fileName);
    }
}
