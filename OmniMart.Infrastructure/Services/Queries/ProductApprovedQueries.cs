using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Services.Queries;

public class ProductApprovedQueries: IProductApprovedQueries
{
    private readonly IAppDbContext _Context;
    public ProductApprovedQueries(IAppDbContext context)
    {
        _Context = context;
    }
    public async Task<ProductApprovedDTO?> GetProductAndVendorDetailsAsync(Guid productId, Guid vendorId)
    {
        var result = await _Context.Products
            .AsNoTracking()
         .Where(p => p.Id == productId && p.VendorId == vendorId)
        .Select(p => new ProductApprovedDTO(
            p.Name,                      
            p.VendorProfile!.User!.Email ?? ""     
        ))
        .FirstOrDefaultAsync();

        return result;
    }
}
