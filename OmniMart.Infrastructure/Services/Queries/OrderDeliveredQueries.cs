using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.Orders.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Services.Queries;

public class OrderDeliveredQueries : IOrderDeliveredQueries
{
    private readonly IAppDbContext _context;
    public OrderDeliveredQueries(IAppDbContext context) {
        _context = context;
    }
    public async Task<OrderDeliveredEventDTOs?> GetCustomerContactInfoAsync(Guid orderId)
    {
        var result = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
    .Select(o => new OrderDeliveredEventDTOs(
      o.Customer!.User!.Email ?? ""
        )).FirstOrDefaultAsync();
          return result;
    }

}
