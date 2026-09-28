using CommerceManager.Domain.Orders;
using MediatR;
namespace CommerceManager.Application.Orders;
public sealed record CreateRetailSale(decimal TotalAmount) : IRequest<Guid>;
public sealed class CreateRetailSaleHandler(IOrderStore store) : IRequestHandler<CreateRetailSale, Guid>
{
    public async Task<Guid> Handle(CreateRetailSale request, CancellationToken cancellationToken)
    {
        var order = Order.Create(SalesChannel.Retail, null, DateTimeOffset.UtcNow, request.TotalAmount);
        await store.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
public interface IOrderStore { Task AddAsync(Order order, CancellationToken cancellationToken); }
