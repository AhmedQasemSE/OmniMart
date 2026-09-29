using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Services.Queries;

public class ProductReviewedQueries : IProductReviewedQueries
{
    private readonly IAppDbContext _context;
    public ProductReviewedQueries(IAppDbContext context) => _context = context;

    public async Task<ProductReviewedEventDTO?> GetReviewDetailsAsync(Guid reviewId)
    {
        return await _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.Id == reviewId)
            .Select(r => new ProductReviewedEventDTO(
                r.Product.VendorProfile!.User!.Email,       
                r.Product.Name,                           
                r.Rating,                                
                r.Customer.User!.FirstName                 
            ))
            .FirstOrDefaultAsync();
        
    }
}
