using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Orders.Queries;

public record VendorOrderSummaryDto(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    int TotalItemsCount,
    DateTimeOffset CreatedAt,
    byte[] RowVersion
);

public record GetVendorOrdersQuery(OrderStatus? Status, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedResult<VendorOrderSummaryDto>>>;

public class GetVendorOrdersQueryHandler : IRequestHandler<GetVendorOrdersQuery, Result<PaginatedResult<VendorOrderSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetVendorOrdersQueryHandler> _logger;

    public GetVendorOrdersQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetVendorOrdersQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<VendorOrderSummaryDto>>> Handle(GetVendorOrdersQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<PaginatedResult<VendorOrderSummaryDto>>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var vendorId = await _context.VendorProfiles
            .AsNoTracking()
            .Where(v => v.UserId == userGuid)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (vendorId == null)
            return Result<PaginatedResult<VendorOrderSummaryDto>>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var query = _context.Orders
                    .AsNoTracking()
                    .Where(o => o.VendorId == vendorId.Value);

            if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        query = query.OrderByDescending(o => o.OrderDate);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new VendorOrderSummaryDto(
                o.Id,
                o.Status.ToString(),
                o.TotalAmount,
                o.OrderItems.Sum(i => i.Quantity),
                o.OrderDate,
                o.RowVersion
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<VendorOrderSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Result<PaginatedResult<VendorOrderSummaryDto>>.Success(paginatedResult);
    }
}

public class GetVendorOrdersQueryValidator : AbstractValidator<GetVendorOrdersQuery>
{
    public GetVendorOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}