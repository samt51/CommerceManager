using CommerceManager.Application.Cash;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/cash")]
public sealed class CashController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetCash(), cancellationToken));
    }

    [HttpPost("inflows")]
    public async Task<IActionResult> AddInflow(
        [FromBody] AddCashInflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddCashInflow(request.Amount, request.Description),
            cancellationToken);

        return result.Success
            ? Ok()
            : BadRequest(new { message = result.Error });
    }

    public sealed class AddCashInflowRequest
    {
        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
