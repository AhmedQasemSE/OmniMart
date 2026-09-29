using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
}

