using CommerceManager.Application.Orders;
using CommerceManager.Domain.Orders;
namespace CommerceManager.Infrastructure.Persistence;
public sealed class OrderStore(AppDbContext db) : IOrderStore
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
    }
}
