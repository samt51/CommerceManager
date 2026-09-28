using CommerceManager.Application.Returns;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/returns")]
public sealed class ReturnsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        [FromQuery] ReturnStatus? status = null,
        [FromQuery] SalesChannel? channel = null,
        CancellationToken cancellationToken = default)
    {
if (page < 0 ||
            size is < 1 or > 100 ||
            page > int.MaxValue / size)
        {
            return BadRequest(
                "page must be >= 0 and size between 1 and 100.");
        }

        return Ok(
            await sender.Send(
                new ListReturns(
                    page,
                    size,
                    status,
                    channel),
                cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
var result =
            await sender.Send(
                new GetReturn(id),
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}
