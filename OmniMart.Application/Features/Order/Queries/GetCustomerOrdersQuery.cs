using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Queries;

public record CustomerOrderSummaryDto(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    int TotalItemsCount,
    DateTimeOffset CreatedAt,
    byte[] RowVersion
);

public record GetCustomerOrdersQuery(int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedResult<CustomerOrderSummaryDto>>>;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<PaginatedResult<CustomerOrderSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetCustomerOrdersQueryHandler> _logger;

    public GetCustomerOrdersQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetCustomerOrdersQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<CustomerOrderSummaryDto>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<PaginatedResult<CustomerOrderSummaryDto>>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var customerId = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userGuid)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerId == null)
            return Result<PaginatedResult<CustomerOrderSummaryDto>>.Failure("Customer profile not found.", ErrorType.NotFound);

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId.Value)
            .OrderByDescending(o => o.OrderDate);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new CustomerOrderSummaryDto(
                o.Id,
                o.Status.ToString(),
                o.TotalAmount,
                o.OrderItems.Sum(i => i.Quantity),
                o.OrderDate,
                o.RowVersion
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<CustomerOrderSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Result<PaginatedResult<CustomerOrderSummaryDto>>.Success(paginatedResult);
    }
    public class GetCustomerOrdersQueryValidator : AbstractValidator<GetCustomerOrdersQuery>
    {
        public GetCustomerOrdersQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Page number must be greater than 0.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100.");
        }
    }
}