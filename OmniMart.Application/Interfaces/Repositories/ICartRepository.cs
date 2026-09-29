using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Interfaces.Repositories;

public interface ICartRepository:IGenericRepository<Cart>
{
    Task<Cart?> GetActiveCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
