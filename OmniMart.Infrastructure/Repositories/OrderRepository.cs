using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }
}