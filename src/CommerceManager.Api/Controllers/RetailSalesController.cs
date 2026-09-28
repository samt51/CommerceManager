using CommerceManager.Application.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace CommerceManager.Api.Controllers;
[ApiController, Route("api/retail-sales")]
public sealed class RetailSalesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRetailSale request, CancellationToken cancellationToken)
    {
        if (request.TotalAmount < 0) return BadRequest("TotalAmount cannot be negative.");
        var id = await sender.Send(request, cancellationToken);
        return Created($"/api/retail-sales/{id}", new { id });
    }
}
