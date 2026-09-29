using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Customers.Queries;

public record CustomerAddressDto(
    Guid Id,
    string Title,
    string City,
    string Street,
    string ZipCode,
    string PhoneNumber
);

public record GetCustomerAddressesQuery : IRequest<Result<List<CustomerAddressDto>>>;

public class GetCustomerAddressesQueryHandler : IRequestHandler<GetCustomerAddressesQuery, Result<List<CustomerAddressDto>>>
{
    private readonly IAppDbContext _context; 
    private readonly ICurrentUserService _currentUserService;

    public GetCustomerAddressesQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<CustomerAddressDto>>> Handle(GetCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<List<CustomerAddressDto>>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var addresses = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userGuid)
            .SelectMany(c => c.Addresses)
            .Select(a => new CustomerAddressDto(
                a.Id,
                a.Title,
                a.City,
                a.Street,
                a.ZipCode,
                a.PhoneNumber
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CustomerAddressDto>>.Success(addresses);
    }
}