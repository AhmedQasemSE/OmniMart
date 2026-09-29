using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;

namespace OmniMart.Infrastructure.Services.Queries;

public class ProductSuspendedQueries : IProductSuspendedQueries
{
    private readonly IAppDbContext _context;
    public ProductSuspendedQueries(IAppDbContext context) => _context = context;

    public async Task<ProductSuspendedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.VendorId == vendorId)
            .Select(p => new ProductSuspendedDTO(
                p.Name,
                p.VendorProfile!.User!.Email ?? ""))
            .FirstOrDefaultAsync();
    }
}