using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Customers.Queries;

public record CustomerProfileDto(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string AccountNumber,
    int LoyaltyPoints,
    decimal TotalSpent
);

public record GetCustomerProfileQuery() : IRequest<Result<CustomerProfileDto>>;

public class GetCustomerProfileQueryHandler : IRequestHandler<GetCustomerProfileQuery, Result<CustomerProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCustomerProfileQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CustomerProfileDto>> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<CustomerProfileDto>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var profile = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userGuid)
            .Select(c => new CustomerProfileDto(
                c.Id,
                c.User!.FirstName,
                c.User.LastName,
                c.User.Email,
                c.User.PhoneNumber!,
                c.User.AccountNumber,
                c.LoyaltyPoints,
                c.TotalSpent
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
            return Result<CustomerProfileDto>.Failure("Profile not found.", ErrorType.NotFound);

        return Result<CustomerProfileDto>.Success(profile);
    }
}