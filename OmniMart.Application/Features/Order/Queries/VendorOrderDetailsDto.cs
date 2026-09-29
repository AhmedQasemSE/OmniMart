using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Queries;

public record VendorOrderDetailsDto(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset OrderDate,
    string PaymentMethod,
    AddressDto ShippingAddress,
    byte[] RowVersion,
    List<OrderItemDetailDto> Items 
);

public record GetVendorOrderDetailsQuery(Guid OrderId) : IRequest<Result<VendorOrderDetailsDto>>;

public class GetVendorOrderDetailsQueryHandler : IRequestHandler<GetVendorOrderDetailsQuery, Result<VendorOrderDetailsDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetVendorOrderDetailsQueryHandler> _logger;

    public GetVendorOrderDetailsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetVendorOrderDetailsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<VendorOrderDetailsDto>> Handle(GetVendorOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<VendorOrderDetailsDto>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var vendorId = await _context.VendorProfiles
            .AsNoTracking()
            .Where(v => v.UserId == userGuid)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (vendorId == null)
            return Result<VendorOrderDetailsDto>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.OrderId && o.VendorId == vendorId.Value)
            .Select(o => new VendorOrderDetailsDto(
                o.Id,
                o.Status.ToString(),
                o.TotalAmount,
                o.OrderDate,
                o.PaymentMethod.ToString(),
                new AddressDto(
                    o.ShippingAddress.City,
                    o.ShippingAddress.Street,
                    o.ShippingAddress.ZipCode,
                    o.ShippingAddress.PhoneNumber
                ),
                o.RowVersion,
                o.OrderItems.Select(i => new OrderItemDetailDto(
                    i.ProductVariantId,
                    i.ProductVariant!.Product!.Name,
                    i.ProductVariant.SKU,
                    i.Price,
                    i.Quantity
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            return Result<VendorOrderDetailsDto>.Failure("Order not found or you do not have permission to view it.", ErrorType.NotFound);

        return Result<VendorOrderDetailsDto>.Success(order);
    }
}

public class GetVendorOrderDetailsQueryValidator : AbstractValidator<GetVendorOrderDetailsQuery>
{
    public GetVendorOrderDetailsQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
    }
}