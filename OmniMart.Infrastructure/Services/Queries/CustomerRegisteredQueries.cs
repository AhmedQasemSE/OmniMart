using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Features.Orders.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Services.Queries;

public class CustomerRegisteredQueries : ICustomerRegisteredQueries
{
    private readonly IAppDbContext _context;
        public CustomerRegisteredQueries(IAppDbContext context) 
    {
        _context = context;
    }
    public async Task<CustomerRegisteredEventDto?> GetCustomerRegisteredEventDtoAsync(Guid id)
    {
        var result = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerRegisteredEventDto(c.User!.Email))
            .FirstOrDefaultAsync();
        return result;
    }

}
