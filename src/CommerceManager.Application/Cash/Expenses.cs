using MediatR;

namespace CommerceManager.Application.Cash;

public sealed record ExpenseTypeRow(Guid Id, string Name);

public sealed record ExpenseRow(
    Guid Id,
    string TypeName,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt);

public sealed record ExpensePage(
    decimal Total,
    IReadOnlyList<ExpenseTypeRow> Types,
    IReadOnlyList<ExpenseRow> Items);

public sealed record CashMovementRow(
    Guid Id,
    int Direction,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt);

public sealed record CashPage(
    decimal Balance,
    decimal TotalIn,
    decimal TotalOut,
    IReadOnlyList<CashMovementRow> Items);

public sealed record LedgerCommandResult(bool Success, string? Error);

public interface IExpenseReader
{
    Task<ExpensePage> ListAsync(
        Guid? expenseTypeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExpenseTypeRow>> ListTypesAsync(
        CancellationToken cancellationToken);
}

public interface IExpenseStore
{
    Task<LedgerCommandResult> AddTypeAsync(
        string name,
        CancellationToken cancellationToken);

    Task<LedgerCommandResult> AddExpenseAsync(
        Guid expenseTypeId,
        decimal amount,
        string description,
        CancellationToken cancellationToken);
}

public interface ICashReader
{
    Task<CashPage> GetAsync(CancellationToken cancellationToken);
}

public interface ICashStore
{
    Task<LedgerCommandResult> AddInflowAsync(
        decimal amount,
        string description,
        CancellationToken cancellationToken);
}

public sealed record ListExpenses(Guid? ExpenseTypeId = null) : IRequest<ExpensePage>;

public sealed class ListExpensesHandler(IExpenseReader reader)
    : IRequestHandler<ListExpenses, ExpensePage>
{
    public Task<ExpensePage> Handle(
        ListExpenses request,
        CancellationToken cancellationToken) =>
        reader.ListAsync(request.ExpenseTypeId, cancellationToken);
}

public sealed record CreateExpenseType(string Name) : IRequest<LedgerCommandResult>;

public sealed class CreateExpenseTypeHandler(IExpenseStore store)
    : IRequestHandler<CreateExpenseType, LedgerCommandResult>
{
    public Task<LedgerCommandResult> Handle(
        CreateExpenseType request,
        CancellationToken cancellationToken) =>
        store.AddTypeAsync(request.Name, cancellationToken);
}

public sealed record CreateExpense(
    Guid ExpenseTypeId,
    decimal Amount,
    string Description) : IRequest<LedgerCommandResult>;

public sealed class CreateExpenseHandler(IExpenseStore store)
    : IRequestHandler<CreateExpense, LedgerCommandResult>
{
    public Task<LedgerCommandResult> Handle(
        CreateExpense request,
        CancellationToken cancellationToken) =>
        store.AddExpenseAsync(
            request.ExpenseTypeId,
            request.Amount,
            request.Description,
            cancellationToken);
}

public sealed record GetCash() : IRequest<CashPage>;

public sealed class GetCashHandler(ICashReader reader)
    : IRequestHandler<GetCash, CashPage>
{
    public Task<CashPage> Handle(
        GetCash request,
        CancellationToken cancellationToken) =>
        reader.GetAsync(cancellationToken);
}

public sealed record AddCashInflow(decimal Amount, string Description)
    : IRequest<LedgerCommandResult>;

public sealed class AddCashInflowHandler(ICashStore store)
    : IRequestHandler<AddCashInflow, LedgerCommandResult>
{
    public Task<LedgerCommandResult> Handle(
        AddCashInflow request,
        CancellationToken cancellationToken) =>
        store.AddInflowAsync(
            request.Amount,
            request.Description,
            cancellationToken);
}
