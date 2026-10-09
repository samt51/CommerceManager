using CommerceManager.Application.Cash;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? expenseTypeId,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(
            new ListExpenses(expenseTypeId),
            cancellationToken));
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreateType(
        [FromBody] CreateExpenseTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateExpenseType(request.Name),
            cancellationToken);

        return result.Success
            ? Ok()
            : BadRequest(new { message = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateExpense(
                request.ExpenseTypeId,
                request.Amount,
                request.Description),
            cancellationToken);

        return result.Success
            ? Ok()
            : BadRequest(new { message = result.Error });
    }

    public sealed class CreateExpenseTypeRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class CreateExpenseRequest
    {
        public Guid ExpenseTypeId { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
