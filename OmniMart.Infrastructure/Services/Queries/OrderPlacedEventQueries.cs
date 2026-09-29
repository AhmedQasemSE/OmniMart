using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services.Queries;

public class OrderPlacedEventQueries : IOrderPlacedEventQueries
{
    private readonly IAppDbContext _context;

    public OrderPlacedEventQueries(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderPlacedEventDTOs?> GetOrderPlacedEventDetailsAsync(Guid orderId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderPlacedEventDTOs(
                o.Customer!.User!.Email ?? "",
                o.VendorProfile!.User!.Email ?? ""
            )).FirstOrDefaultAsync();
    }
}