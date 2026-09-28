using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using MediatR;

namespace CommerceManager.Application.Returns;

public sealed record ReturnRow(
    Guid Id,
    string ExternalReturnId,
    Guid OrderId,
    string? OrderNumber,
    SalesChannel Channel,
    string? Barcode,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    ReturnStatus Status,
    bool StockApplied,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset ExternalLastModifiedAt,
    DateTimeOffset CreatedAt);

public sealed record ReturnDetails(
    Guid Id,
    string ExternalReturnId,
    Guid OrderId,
    Guid OrderItemId,
    string? OrderNumber,
    string? ExternalOrderId,
    SalesChannel Channel,
    string? Barcode,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    ReturnStatus Status,
    bool StockApplied,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset ExternalLastModifiedAt,
    DateTimeOffset CreatedAt);

public sealed record ReturnPage(
    int Page,
    int Size,
    int Total,
    IReadOnlyList<ReturnRow> Items);

public interface IReturnReader
{
    Task<ReturnPage> ListAsync(
        int page,
        int size,
        ReturnStatus? status,
        SalesChannel? channel,
        CancellationToken cancellationToken);

    Task<ReturnDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record ListReturns(
    int Page = 0,
    int Size = 25,
    ReturnStatus? Status = null,
    SalesChannel? Channel = null)
    : IRequest<ReturnPage>;

public sealed record GetReturn(Guid Id)
    : IRequest<ReturnDetails?>;

public sealed class ListReturnsHandler(IReturnReader reader)
    : IRequestHandler<ListReturns, ReturnPage>
{
    public Task<ReturnPage> Handle(
        ListReturns request,
        CancellationToken cancellationToken)
        => reader.ListAsync(
            request.Page,
            request.Size,
            request.Status,
            request.Channel,
            cancellationToken);
}

public sealed class GetReturnHandler(IReturnReader reader)
    : IRequestHandler<GetReturn, ReturnDetails?>
{
    public Task<ReturnDetails?> Handle(
        GetReturn request,
        CancellationToken cancellationToken)
        => reader.GetAsync(
            request.Id,
            cancellationToken);
}
