using CommerceManager.Application.Ledger;
using CommerceManager.Domain.Ledger;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/current-accounts")]
public sealed class CurrentAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(
            new ListCurrentAccounts(search),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await sender.Send(
            new GetCurrentAccount(id),
            cancellationToken);

        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCurrentAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(CurrentAccountKind), request.Kind))
            return BadRequest(new { message = "Cari tipi geçersiz." });

        var result = await sender.Send(
            new CreateCurrentAccount(
                request.Name,
                (CurrentAccountKind)request.Kind,
                request.Phone,
                request.TaxNumber),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok();
    }

    [HttpPost("{id:guid}/entries")]
    public async Task<IActionResult> AddEntry(
        Guid id,
        [FromBody] AddCurrentAccountEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(LedgerDirection), request.Direction))
            return BadRequest(new { message = "Hareket tipi geçersiz." });

        var result = await sender.Send(
            new AddCurrentAccountEntry(
                id,
                (LedgerDirection)request.Direction,
                request.Amount,
                request.Description),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok();
    }

    public sealed class CreateCurrentAccountRequest
    {
        public string Name { get; set; } = string.Empty;

        public int Kind { get; set; }

        public string? Phone { get; set; }

        public string? TaxNumber { get; set; }
    }

    public sealed class AddCurrentAccountEntryRequest
    {
        public int Direction { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
