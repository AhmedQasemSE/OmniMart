
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Repositories;
public class CartRepository:GenericRepository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context)
    {
    }
    public async Task<Cart?> GetActiveCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv!.Product) 
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }
}

