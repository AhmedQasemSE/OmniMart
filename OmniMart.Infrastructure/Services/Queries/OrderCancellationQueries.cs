using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.Orders.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Queries;

public class OrderCancellationQueries : IOrderCancellationQueries
{
    private readonly IAppDbContext _context;

    public OrderCancellationQueries(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderCancellationEmailsDto?> GetEmailsAsync(Guid orderId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderCancellationEmailsDto(
                o.Customer!.User!.Email ?? "",
                o.VendorProfile!.User!.Email ?? ""
            ))
            .FirstOrDefaultAsync();
    }
}