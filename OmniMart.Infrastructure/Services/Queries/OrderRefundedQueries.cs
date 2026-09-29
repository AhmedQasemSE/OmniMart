using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.Orders.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services.Queries;

public class OrderRefundedQueries : IOrderRefundedQueries
{
    private readonly IAppDbContext _context;

    public OrderRefundedQueries(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderRefundedEventDTOs?> GetCustomerContactInfoAsync(Guid orderId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderRefundedEventDTOs(
                o.Customer!.User!.Email ?? "",
                o.VendorProfile!.User!.Email ?? ""
            ))
            .FirstOrDefaultAsync();
    }
}