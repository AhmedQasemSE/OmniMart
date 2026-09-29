using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Vendors.Queries;

public record VendorDashboardStatsDto(
    int TotalActiveProducts,
    int LowStockItemsAlert, 
    int PendingOrders,      
    int CompletedOrders,   
    decimal TotalHistoricalRevenue,
    decimal CurrentWalletBalance   
);

public record GetVendorDashboardStatsQuery() : IRequest<Result<VendorDashboardStatsDto>>;

public class GetVendorDashboardStatsQueryHandler : IRequestHandler<GetVendorDashboardStatsQuery, Result<VendorDashboardStatsDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVendorDashboardStatsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<VendorDashboardStatsDto>> Handle(GetVendorDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<VendorDashboardStatsDto>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var vendor = await _context.VendorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == userGuid, cancellationToken);

        if (vendor == null)
            return Result<VendorDashboardStatsDto>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var vendorId = vendor.Id;

        int activeProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.VendorId == vendorId && p.Status == ProductStatus.Active && !p.IsDeleted, cancellationToken);

        int lowStockItems = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Product!.VendorId == vendorId && !v.Product.IsDeleted && v.StockQuantity <= 5)
            .CountAsync(cancellationToken);

        var vendorOrdersQuery = _context.Orders.AsNoTracking().Where(o => o.VendorId == vendorId);

        int pendingOrders = await vendorOrdersQuery
            .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing, cancellationToken);

        int completedOrders = await vendorOrdersQuery
            .CountAsync(o => o.Status == OrderStatus.Delivered, cancellationToken);

        decimal totalRevenue = await vendorOrdersQuery
            .Where(o => o.Status == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var stats = new VendorDashboardStatsDto(
            activeProducts,
            lowStockItems,
            pendingOrders,
            completedOrders,
            totalRevenue,
            vendor.CurrentBalance 
        );

        return Result<VendorDashboardStatsDto>.Success(stats);
    }
}