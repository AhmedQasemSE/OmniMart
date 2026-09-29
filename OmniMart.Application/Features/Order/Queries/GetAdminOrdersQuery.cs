using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Queries;

public record AdminOrderSummaryDto(
    Guid OrderId,
    string CustomerName,
    string VendorStoreName,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    DateTimeOffset OrderDate
);

public record GetAdminOrdersQuery(
    string? SearchTerm = null,
    OrderStatus? StatusFilter = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<AdminOrderSummaryDto>>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, Result<PaginatedResult<AdminOrderSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAdminOrdersQueryHandler> _logger;

    public GetAdminOrdersQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetAdminOrdersQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<AdminOrderSummaryDto>>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            return Result<PaginatedResult<AdminOrderSummaryDto>>.Failure("Only Administrators can view all platform orders.", ErrorType.Unauthorized);
        }

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c!.User)
            .Include(o => o.VendorProfile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(o =>
                (o.Customer != null && o.Customer.User != null && (o.Customer.User.FirstName.ToLower().Contains(term) || o.Customer.User.Email.ToLower().Contains(term))) ||
                (o.VendorProfile != null && o.VendorProfile.StoreName.ToLower().Contains(term)) ||
                o.Id.ToString().ToLower().Contains(term) 
            );
        }

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(o => o.Status == request.StatusFilter.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= request.EndDate.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return Result<PaginatedResult<AdminOrderSummaryDto>>.Success(
                new PaginatedResult<AdminOrderSummaryDto>(new List<AdminOrderSummaryDto>(), 0, request.Page, request.PageSize));
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new AdminOrderSummaryDto(
                o.Id,
                o.Customer!.User!.FirstName + " " + o.Customer.User.LastName,
                o.VendorProfile!.StoreName,
                o.TotalAmount,
                o.Status.ToString(),
                o.PaymentMethod.ToString(),
                o.OrderDate
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<AdminOrderSummaryDto>(orders, totalCount, request.Page, request.PageSize);
        return Result<PaginatedResult<AdminOrderSummaryDto>>.Success(paginatedResult);
    }
}

public class GetAdminOrdersQueryValidator : AbstractValidator<GetAdminOrdersQuery>
{
    public GetAdminOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Start Date must be before or equal to End Date.");
    }
}