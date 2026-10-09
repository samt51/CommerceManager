using CommerceManager.Domain.Ledger;
using MediatR;

namespace CommerceManager.Application.Ledger;

public sealed record CurrentAccountRow(
    Guid Id,
    string Name,
    int Kind,
    string? Phone,
    decimal Balance,
    DateTimeOffset CreatedAt);

public sealed record CurrentAccountPage(
    int Total,
    decimal Receivable,
    decimal Payable,
    IReadOnlyList<CurrentAccountRow> Items);

public sealed record CurrentAccountEntryRow(
    Guid Id,
    int Direction,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt);

public sealed record CurrentAccountDetails(
    Guid Id,
    string Name,
    int Kind,
    string? Phone,
    string? TaxNumber,
    decimal Balance,
    IReadOnlyList<CurrentAccountEntryRow> Entries);

public interface ICurrentAccountReader
{
    Task<CurrentAccountPage> ListAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<CurrentAccountDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record ListCurrentAccounts(string? Search = null)
    : IRequest<CurrentAccountPage>;

public sealed class ListCurrentAccountsHandler(ICurrentAccountReader reader)
    : IRequestHandler<ListCurrentAccounts, CurrentAccountPage>
{
    public Task<CurrentAccountPage> Handle(
        ListCurrentAccounts request,
        CancellationToken cancellationToken) =>
        reader.ListAsync(request.Search, cancellationToken);
}

public sealed record GetCurrentAccount(Guid Id)
    : IRequest<CurrentAccountDetails?>;

public sealed class GetCurrentAccountHandler(ICurrentAccountReader reader)
    : IRequestHandler<GetCurrentAccount, CurrentAccountDetails?>
{
    public Task<CurrentAccountDetails?> Handle(
        GetCurrentAccount request,
        CancellationToken cancellationToken) =>
        reader.GetAsync(request.Id, cancellationToken);
}

public sealed record LedgerResult(bool Success, string? Error);

public interface ICurrentAccountStore
{
    Task<LedgerResult> AddAccountAsync(
        string name,
        CurrentAccountKind kind,
        string? phone,
        string? taxNumber,
        CancellationToken cancellationToken);

    Task<LedgerResult> AddEntryAsync(
        Guid currentAccountId,
        LedgerDirection direction,
        decimal amount,
        string description,
        CancellationToken cancellationToken);
}

public sealed record CreateCurrentAccount(
    string Name,
    CurrentAccountKind Kind,
    string? Phone,
    string? TaxNumber) : IRequest<LedgerResult>;

public sealed class CreateCurrentAccountHandler(ICurrentAccountStore store)
    : IRequestHandler<CreateCurrentAccount, LedgerResult>
{
    public Task<LedgerResult> Handle(
        CreateCurrentAccount request,
        CancellationToken cancellationToken) =>
        store.AddAccountAsync(
            request.Name,
            request.Kind,
            request.Phone,
            request.TaxNumber,
            cancellationToken);
}

public sealed record AddCurrentAccountEntry(
    Guid CurrentAccountId,
    LedgerDirection Direction,
    decimal Amount,
    string Description) : IRequest<LedgerResult>;

public sealed class AddCurrentAccountEntryHandler(ICurrentAccountStore store)
    : IRequestHandler<AddCurrentAccountEntry, LedgerResult>
{
    public Task<LedgerResult> Handle(
        AddCurrentAccountEntry request,
        CancellationToken cancellationToken) =>
        store.AddEntryAsync(
            request.CurrentAccountId,
            request.Direction,
            request.Amount,
            request.Description,
            cancellationToken);
}
