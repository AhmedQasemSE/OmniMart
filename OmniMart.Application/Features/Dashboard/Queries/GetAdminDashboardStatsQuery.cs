using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Dashboard.Queries;

public record AdminDashboardStatsDto(
    decimal TotalPlatformRevenue,   
    int PendingVendorApprovals,     
    int PendingProductApprovals,    
    int TotalActiveCustomers,        
    int TotalActiveVendors,          
    int TotalOrders                 
);

public record GetAdminDashboardStatsQuery() : IRequest<Result<AdminDashboardStatsDto>>;

public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, Result<AdminDashboardStatsDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAdminDashboardStatsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AdminDashboardStatsDto>> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            return Result<AdminDashboardStatsDto>.Failure("Only Administrators can view platform statistics.", ErrorType.Unauthorized);
        }

        decimal totalRevenue = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        int pendingVendors = await _context.VendorProfiles
            .AsNoTracking()
            .CountAsync(v => v.IsApproved == false, cancellationToken);

        int pendingProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.Status == ProductStatus.PendingReview && !p.IsDeleted, cancellationToken);

        int activeCustomers = await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.Role == SystemRole.Customer && u.IsActive, cancellationToken);

        int activeVendors = await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.Role == SystemRole.Vendor && u.IsActive, cancellationToken);

        int totalOrders = await _context.Orders.CountAsync(cancellationToken);

        var stats = new AdminDashboardStatsDto(
            totalRevenue,
            pendingVendors,
            pendingProducts,
            activeCustomers,
            activeVendors,
            totalOrders
        );

        return Result<AdminDashboardStatsDto>.Success(stats);
    }
}