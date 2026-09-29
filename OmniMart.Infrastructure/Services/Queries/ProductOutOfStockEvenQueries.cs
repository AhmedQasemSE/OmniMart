using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;

namespace OmniMart.Infrastructure.Services.Queries;

public class ProductOutOfStockEventsQueries : IProductOutOfStockEventsQueries
{
    private readonly IAppDbContext _context;
    public ProductOutOfStockEventsQueries(IAppDbContext context)
    {
        _context = context;

    }
    public async Task<ProductOutOfStockEventDTo?> GetProductOutOfStockEvent(Guid variantId)
    {
        var result = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.Id == variantId)
                    .Select(v => new ProductOutOfStockEventDTo(
                        v.Product!.VendorProfile!.User!.Email,
                        v.Product.Name,
                        v.SKU
                    ))
                    .FirstOrDefaultAsync();
        return result;
    }
}
