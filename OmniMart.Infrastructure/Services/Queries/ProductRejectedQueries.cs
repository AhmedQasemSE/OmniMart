using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;

namespace OmniMart.Infrastructure.Services.Queries;

public class ProductRejectedQueries : IProductRejectedQueries
{
    private readonly IAppDbContext _context;
    public ProductRejectedQueries(IAppDbContext context) => _context = context;

    public async Task<ProductRejectedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.VendorId == vendorId)
            .Select(p => new ProductRejectedDTO(
                p.Name,
                p.VendorProfile!.User!.Email ?? ""))
            .FirstOrDefaultAsync();
    }
}