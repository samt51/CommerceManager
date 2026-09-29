using MediatR;

namespace CommerceManager.Application.Orders;

public sealed record OrderRow(Guid Id, string? ExternalOrderId, string? OrderNumber,
    string? Status, DateTimeOffset OrderedAt, decimal TotalAmount, int ItemCount,
    string? FirstProductName, string? FirstProductImageUrl);
public sealed record OrderLineRow(string ExternalLineId, string? Barcode,
    string ProductName, string? ImageUrl, int Quantity, decimal UnitPrice);
public sealed record OrderDetails(Guid Id, string? ExternalOrderId,
    string? OrderNumber, string? Status, DateTimeOffset OrderedAt,
    DateTimeOffset? ExternalLastModifiedAt, decimal TotalAmount,
    IReadOnlyList<OrderLineRow> Items);
public sealed record OrderPage(int Page, int Size, int Total, IReadOnlyList<OrderRow> Items);

public interface IOrderReader
{
    Task<OrderPage> ListTrendyolAsync(int page, int size, string? status,
        CancellationToken cancellationToken);
    Task<OrderDetails?> GetTrendyolAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record ListTrendyolOrders(int Page = 0, int Size = 25,
    string? Status = null) : IRequest<OrderPage>;
public sealed class ListTrendyolOrdersHandler(IOrderReader reader)
    : IRequestHandler<ListTrendyolOrders, OrderPage>
{
    public Task<OrderPage> Handle(ListTrendyolOrders request,
        CancellationToken cancellationToken) => reader.ListTrendyolAsync(
            request.Page, request.Size, request.Status, cancellationToken);
}

public sealed record GetTrendyolOrder(Guid Id) : IRequest<OrderDetails?>;
public sealed class GetTrendyolOrderHandler(IOrderReader reader)
    : IRequestHandler<GetTrendyolOrder, OrderDetails?>
{
    public Task<OrderDetails?> Handle(GetTrendyolOrder request,
        CancellationToken cancellationToken) => reader.GetTrendyolAsync(
            request.Id, cancellationToken);
}
